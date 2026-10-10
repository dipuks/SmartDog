using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;

namespace SmartDog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/decisions/{decisionId:guid}/alternatives/{alternativeId:guid}/ratings")]
public class DecisionAlternativeRatingsController : ControllerBase
{
    private readonly IDecisionAlternativeRatingService _service;

    public DecisionAlternativeRatingsController(
        IDecisionAlternativeRatingService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<DecisionAlternativeRatingDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DecisionAlternativeRatingDto>>>
        GetRatings(Guid decisionId, Guid alternativeId)
    {
        var ratings = await _service.GetAsync(decisionId, alternativeId);
        return ratings is null ? NotFound() : Ok(ratings);
    }

    [HttpPut("{criterionId:guid}")]
    [ProducesResponseType(
        typeof(DecisionAlternativeRatingDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecisionAlternativeRatingDto>> SetRating(
        Guid decisionId,
        Guid alternativeId,
        Guid criterionId,
        UpdateDecisionAlternativeRatingRequest request)
    {
        var rating = await _service.SetAsync(
            decisionId,
            alternativeId,
            criterionId,
            request);

        return rating is null ? NotFound() : Ok(rating);
    }
}