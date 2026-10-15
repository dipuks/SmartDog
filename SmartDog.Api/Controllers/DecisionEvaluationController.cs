using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using SmartDog.Api.Security;
using SmartDog.Domain.Evaluation;

namespace SmartDog.Api.Controllers;

/// <summary>
/// Stateless decision evaluation for service clients such as CQ Pulse: ranks the submitted options with the transparent
/// weighted-criteria method of <see cref="WeightedDecisionEvaluator"/>. Nothing is stored and no AI is involved.
/// Authenticated with an API key (<c>X-Api-Key</c>) only: user JWTs are not accepted here, and API keys are not
/// accepted on the user endpoints.
/// </summary>
[ApiController]
[Route("api/decisions/evaluate")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public sealed class DecisionEvaluationController(
    WeightedDecisionEvaluator evaluator,
    ILogger<DecisionEvaluationController> logger) : ControllerBase
{
    /// <summary>Largest accepted request body; the evaluator's own limits (20 criteria, 200 options) fit well within it.</summary>
    public const int MaxRequestBytes = 256 * 1024;
    public const string RateLimitPolicy = "decision-evaluate";

    /// <summary>
    /// 200 with a ranking (status <c>Ranked</c>) or with every option excluded by hard constraints (status
    /// <c>NoFeasibleOption</c>); 400 with field errors for invalid input; 401 without a valid key, 503 when no key is
    /// configured; 413 above <see cref="MaxRequestBytes"/>; 429 when rate limited.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RejectLargeBodies(MaxRequestBytes)]
    [ProducesResponseType<EvaluationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public ActionResult<EvaluationResult> Evaluate(EvaluationRequest request)
    {
        try
        {
            var result = evaluator.Evaluate(request);
            logger.LogInformation("Evaluation {EvaluationId}: {Status}, {Ranked} ranked, {Infeasible} infeasible, {Criteria} criteria.",
                result.EvaluationId, result.Status, result.Ranking.Count, result.Infeasible.Count, result.Criteria.Count);
            return Ok(result);
        }
        catch (EvaluationValidationException exception)
        {
            return ValidationProblem(new ValidationProblemDetails(exception.Errors.ToDictionary(x => x.Key, x => x.Value))
            {
                Title = "The evaluation request is invalid."
            });
        }
    }

    /// <summary>
    /// Refuses a declared body larger than the limit before it is read or bound. <see cref="RequestSizeLimitAttribute"/>
    /// also stops chunked bodies on Kestrel; this check applies on every server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class RejectLargeBodiesAttribute(long maxBytes) : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context.HttpContext.Request.ContentLength > maxBytes)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status413PayloadTooLarge,
                    Title = "The request body is too large.",
                    Detail = $"Evaluation requests are limited to {maxBytes / 1024} KB."
                }) { StatusCode = StatusCodes.Status413PayloadTooLarge };
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
