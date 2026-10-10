namespace SmartDog.Application.DTOs;

public record DecisionQueryRequest(
    string? Status,
    string? SortBy,
    string? SortDirection,
    int Page = 1,
    int PageSize = 20);