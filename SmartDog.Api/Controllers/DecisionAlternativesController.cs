using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;

namespace SmartDog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/decisions/{decisionId:guid}/alternatives")]
public class DecisionAlternativesController : ControllerBase
{
    private readonly IDecisionAlternativeService _service;

    public DecisionAlternativesController(
        IDecisionAlternativeService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<DecisionAlternativeDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DecisionAlternativeDto>>>
        GetAlternatives(Guid decisionId)
    {
        var alternatives =
            await _service.GetByDecisionIdAsync(decisionId);

        return Ok(alternatives);
    }

    [HttpGet("{alternativeId:guid}")]
    [ProducesResponseType(
        typeof(DecisionAlternativeDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionAlternativeDto>>
        GetAlternative(
            Guid decisionId,
            Guid alternativeId)
    {
        var alternative =
            await _service.GetByIdAsync(
                decisionId,
                alternativeId);

        if (alternative is null)
            return NotFound();

        return Ok(alternative);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(DecisionAlternativeDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecisionAlternativeDto>>
        CreateAlternative(
            Guid decisionId,
            CreateDecisionAlternativeRequest request)
    {
        var alternative =
            await _service.CreateAsync(
                decisionId,
                request);

        return CreatedAtAction(
            nameof(GetAlternative),
            new
            {
                decisionId,
                alternativeId = alternative!.Id
            },
            alternative);
    }

    [HttpPut("{alternativeId:guid}")]
    [ProducesResponseType(
        typeof(DecisionAlternativeDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionAlternativeDto>>
        UpdateAlternative(
            Guid decisionId,
            Guid alternativeId,
            UpdateDecisionAlternativeRequest request)
    {
        var alternative =
            await _service.UpdateAsync(
                decisionId,
                alternativeId,
                request);

        if (alternative is null)
            return NotFound();

        return Ok(alternative);
    }

    [HttpDelete("{alternativeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAlternative(
        Guid decisionId,
        Guid alternativeId)
    {
        var deleted =
            await _service.DeleteAsync(
                decisionId,
                alternativeId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}