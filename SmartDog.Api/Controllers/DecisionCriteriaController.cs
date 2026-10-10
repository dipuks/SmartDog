using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;

namespace SmartDog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/decisions/{decisionId:guid}/criteria")]
public class DecisionCriteriaController : ControllerBase
{
    private readonly IDecisionCriterionService _service;

    public DecisionCriteriaController(IDecisionCriterionService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<DecisionCriterionDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DecisionCriterionDto>>>
        GetCriteria(Guid decisionId)
    {
        var criteria = await _service.GetByDecisionIdAsync(decisionId);
        return criteria is null ? NotFound() : Ok(criteria);
    }

    [HttpGet("{criterionId:guid}")]
    [ProducesResponseType(typeof(DecisionCriterionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionCriterionDto>> GetCriterion(
        Guid decisionId,
        Guid criterionId)
    {
        var criterion = await _service.GetByIdAsync(decisionId, criterionId);
        return criterion is null ? NotFound() : Ok(criterion);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DecisionCriterionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionCriterionDto>> CreateCriterion(
        Guid decisionId,
        CreateDecisionCriterionRequest request)
    {
        var criterion = await _service.CreateAsync(decisionId, request);

        if (criterion is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetCriterion),
            new { decisionId, criterionId = criterion.Id },
            criterion);
    }

    [HttpPut("{criterionId:guid}")]
    [ProducesResponseType(typeof(DecisionCriterionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionCriterionDto>> UpdateCriterion(
        Guid decisionId,
        Guid criterionId,
        UpdateDecisionCriterionRequest request)
    {
        var criterion = await _service.UpdateAsync(
            decisionId,
            criterionId,
            request);

        return criterion is null ? NotFound() : Ok(criterion);
    }

    [HttpDelete("{criterionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCriterion(
        Guid decisionId,
        Guid criterionId)
    {
        var deleted = await _service.DeleteAsync(decisionId, criterionId);
        return deleted ? NoContent() : NotFound();
    }
}