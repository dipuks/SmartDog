using Microsoft.EntityFrameworkCore;
using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

using Microsoft.Extensions.Caching.Memory;

public class DecisionQueryService : IDecisionQueryService
{
    private readonly IDecisionRepository _repository;
    private readonly IMemoryCache _cache;

    public DecisionQueryService(
        IDecisionRepository repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PagedResult<DecisionDto>> GetAllAsync(
        DecisionQueryRequest request)
    {
        var page = request.Page < 1
            ? 1
            : request.Page;

        var pageSize = request.PageSize < 1
            ? 20
            : Math.Min(request.PageSize, 100);

        var query = _repository.Query();

        // -------------------------
        // Filtering
        // -------------------------

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(d =>
                d.Status == request.Status);
        }

        // -------------------------
        // Total count
        // -------------------------

        var totalCount = await query.CountAsync();

        // -------------------------
        // Sorting
        // -------------------------

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "title" => request.SortDirection == "desc"
                ? query.OrderByDescending(d => d.Title)
                : query.OrderBy(d => d.Title),

            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(d => d.Status)
                : query.OrderBy(d => d.Status),

            "createdat" => request.SortDirection == "asc"
                ? query.OrderBy(d => d.CreatedAt)
                : query.OrderByDescending(d => d.CreatedAt),

            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        // -------------------------
        // Pagination
        // -------------------------

        var skip = (page - 1) * pageSize;

        var decisions = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // -------------------------
        // Map to DTOs
        // -------------------------

        var items = decisions
            .Select(d => new DecisionDto(
                d.Id,
                d.Title,
                d.Description,
                d.CreatedAt,
                d.Status,
                d.RowVersion))
            .ToList();

        return new PagedResult<DecisionDto>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<DecisionDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"decision:{id}";

        if (_cache.TryGetValue(cacheKey, out DecisionDto? cachedDecision))
        {
            return cachedDecision;
        }

        var decision = await _repository.GetByIdAsync(id);

        if (decision is null)
        {
            return null;
        }

        var dto = new DecisionDto(
            decision.Id,
            decision.Title,
            decision.Description,
            decision.CreatedAt,
            decision.Status,
            decision.RowVersion);

        _cache.Set(
            cacheKey,
            dto,
            TimeSpan.FromMinutes(5));

        return dto;
    }
}