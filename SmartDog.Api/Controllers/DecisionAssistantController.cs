using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartDog.Api.Services;

namespace SmartDog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/decisions/{decisionId:guid}/assistant")]
public sealed class DecisionAssistantController : ControllerBase
{
    private readonly SmartDogDbContext _dbContext;
    private readonly DecisionAssistantService _assistant;

    public DecisionAssistantController(
        SmartDogDbContext dbContext,
        DecisionAssistantService assistant)
    {
        _dbContext = dbContext;
        _assistant = assistant;
    }

    [HttpPost]
    [EnableRateLimiting("ai-assistant")]
    [ProducesResponseType(typeof(DecisionAssistantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DecisionAssistantResponse>> Ask(
        Guid decisionId,
        DecisionAssistantRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = Request.Headers["X-OpenAI-Api-Key"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 256)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "OpenAI API key required",
                Detail = "Enter your OpenAI API key in the assistant settings to continue."
            });
        }

        var decision = await _dbContext.Decisions
            .AsNoTracking()
            .Where(item => item.Id == decisionId)
            .Select(item => new
            {
                item.Title,
                item.Description,
                item.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (decision is null)
        {
            return NotFound();
        }

        var criteria = await _dbContext.DecisionCriteria
            .AsNoTracking()
            .Where(item => item.DecisionId == decisionId)
            .OrderBy(item => item.Name)
            .Take(31)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Weight
            })
            .ToListAsync(cancellationToken);

        var alternatives = await _dbContext.DecisionAlternatives
            .AsNoTracking()
            .Where(item => item.DecisionId == decisionId)
            .OrderBy(item => item.Name)
            .Take(31)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Description
            })
            .ToListAsync(cancellationToken);

        if (criteria.Count > 30 || alternatives.Count > 30)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Decision is too large for AI assistance",
                Detail = "The assistant supports up to 30 criteria and 30 alternatives per decision."
            });
        }

        var criterionIds = criteria.Select(item => item.Id).ToArray();
        var alternativeIds = alternatives.Select(item => item.Id).ToArray();
        var ratings = await _dbContext.DecisionAlternativeCriterionRatings
            .AsNoTracking()
            .Where(item =>
                item.DecisionId == decisionId &&
                criterionIds.Contains(item.DecisionCriterionId) &&
                alternativeIds.Contains(item.DecisionAlternativeId))
            .Select(item => new
            {
                item.DecisionAlternativeId,
                item.DecisionCriterionId,
                item.Rating
            })
            .ToListAsync(cancellationToken);

        var ratingsByPair = ratings.ToDictionary(
            item => (item.DecisionAlternativeId, item.DecisionCriterionId),
            item => item.Rating);
        var scores = criteria.Count > 0 &&
                     criteria.All(item => item.Weight is >= 0 and <= 100) &&
                     criteria.Sum(item => item.Weight) == 100m &&
                     alternatives.All(alternative =>
                         criteria.All(criterion =>
                             ratingsByPair.TryGetValue(
                                 (alternative.Id, criterion.Id),
                                 out var rating) &&
                             rating is >= 0 and <= 100)
                     )
            ? alternatives.Select(alternative => new
                {
                    alternative.Name,
                    Score = Math.Round(
                        criteria.Sum(criterion =>
                            criterion.Weight *
                            ratingsByPair[(alternative.Id, criterion.Id)] / 100m),
                        2,
                        MidpointRounding.AwayFromZero)
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : null;

        var context = JsonSerializer.Serialize(new
        {
            decision.Title,
            decision.Description,
            decision.Status,
            Criteria = criteria.Select(item => new
            {
                item.Name,
                item.Weight
            }),
            Alternatives = alternatives.Select(item => new
            {
                item.Name,
                Description = item.Description is null
                    ? null
                    : item.Description[..Math.Min(item.Description.Length, 300)],
                Ratings = criteria
                    .Where(criterion =>
                        ratingsByPair.ContainsKey((item.Id, criterion.Id)))
                    .Select(criterion => new
                    {
                        Criterion = criterion.Name,
                        Rating = ratingsByPair[(item.Id, criterion.Id)]
                    })
            }),
            WeightedRanking = scores
        });

        try
        {
            var answer = await _assistant.AskAsync(
                apiKey,
                request.Question,
                context,
                cancellationToken);
            return Ok(new DecisionAssistantResponse(answer));
        }
        catch (AssistantInvalidKeyException)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "OpenAI API key was rejected",
                Detail = "Check the key in assistant settings and try again."
            });
        }
        catch (AssistantProviderException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI assistant is temporarily unavailable",
                detail: "The AI provider could not complete the request. Try again later.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI assistant is temporarily unavailable",
                detail: "The API server could not reach the AI provider.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "AI assistant request timed out",
                detail: "Try again with a shorter question.");
        }
    }
}

public sealed record DecisionAssistantRequest(
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(1000, MinimumLength = 3)]
    string Question);

public sealed record DecisionAssistantResponse(string Answer);
