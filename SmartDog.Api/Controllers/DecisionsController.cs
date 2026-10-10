using Microsoft.AspNetCore.Mvc;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace SmartDog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DecisionsController : ControllerBase
{
    private readonly IDecisionQueryService _decisionQueryService;
    private readonly IDecisionCommandService _decisionCommandService;

    public DecisionsController(
        IDecisionQueryService decisionQueryService,
        IDecisionCommandService decisionCommandService)
    {
        _decisionQueryService = decisionQueryService;
        _decisionCommandService = decisionCommandService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DecisionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecisionDto>> CreateDecision(
    CreateDecisionRequest request)
    {
        var decision = await _decisionCommandService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetDecisionById),
            new { id = decision.Id },
            decision);
    }


    [HttpGet]
    [ProducesResponseType(
    typeof(PagedResult<DecisionDto>),
    StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DecisionDto>>> GetDecisions(
    [FromQuery] DecisionQueryRequest request)
    {
        var result = await _decisionQueryService.GetAllAsync(request);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
    typeof(DecisionDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDto>> GetDecisionById(Guid id)
    {
        var decision = await _decisionQueryService.GetByIdAsync(id);

        if (decision is null)
            return NotFound();

        return Ok(decision);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
    typeof(DecisionDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecisionDto>> UpdateDecision(
    Guid id,
    UpdateDecisionRequest request)
    {
        var decision = await _decisionCommandService.UpdateAsync(
            id,
            request);

        if (decision is null)
            return NotFound();

        return Ok(decision);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDecision(Guid id)
    {
        var deleted =
            await _decisionCommandService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}