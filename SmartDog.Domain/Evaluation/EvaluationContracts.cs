using System.Text.Json.Serialization;

namespace SmartDog.Domain.Evaluation;

/// <summary>
/// A structured decision: rank <see cref="Options"/> by weighted <see cref="Criteria"/>, excluding options that violate any
/// hard <see cref="Constraints"/>. Every option must provide a numeric value for every criterion.
/// </summary>
public sealed class EvaluationRequest
{
    public string? Question { get; set; }
    public List<EvaluationCriterion>? Criteria { get; set; }
    public List<EvaluationOption>? Options { get; set; }
    public List<EvaluationConstraint>? Constraints { get; set; }
}

public sealed class EvaluationCriterion
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    /// <summary>Relative importance; must be greater than zero. Weights are normalized to sum to 1.</summary>
    public double? Weight { get; set; }
    /// <summary>"maximize" (higher is better, the default) or "minimize" (lower is better).</summary>
    public string? Direction { get; set; }
}

public sealed class EvaluationOption
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    /// <summary>Criterion id → value. Must contain exactly the request's criteria.</summary>
    public Dictionary<string, double?>? Values { get; set; }
}

/// <summary>A hard constraint: an option is infeasible unless <c>value(CriterionId) Operator Value</c> holds.</summary>
public sealed class EvaluationConstraint
{
    public string? Id { get; set; }
    public string? CriterionId { get; set; }
    /// <summary>One of &lt;, &lt;=, &gt;, &gt;=, ==, !=.</summary>
    public string? Operator { get; set; }
    public double? Value { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<EvaluationStatus>))]
public enum EvaluationStatus
{
    /// <summary>At least one option satisfied every hard constraint and was ranked.</summary>
    Ranked,
    /// <summary>Every option violated at least one hard constraint; nothing was ranked.</summary>
    NoFeasibleOption
}

public sealed record EvaluationResult(
    Guid EvaluationId,
    EvaluationStatus Status,
    string? Question,
    string Method,
    IReadOnlyList<WeightedCriterion> Criteria,
    IReadOnlyList<RankedOption> Ranking,
    IReadOnlyList<InfeasibleOption> Infeasible,
    IReadOnlyList<string> Notes,
    string Limitations);

/// <summary>A criterion with its submitted weight, its normalized share of the total weight, and the observed range.</summary>
public sealed record WeightedCriterion(
    string Id, string Name, string Direction, double Weight, double NormalizedWeight, double? MinimumValue, double? MaximumValue);

public sealed record RankedOption(
    int Rank,
    string OptionId,
    string Name,
    double Score,
    IReadOnlyList<CriterionContribution> Contributions,
    string Explanation,
    IReadOnlyList<string> TiedWithOptionIds);

/// <summary>How one criterion contributed: normalized value (0–1, best = 1) × normalized weight = contribution to the score.</summary>
public sealed record CriterionContribution(
    string CriterionId, double RawValue, double NormalizedValue, double NormalizedWeight, double Contribution);

public sealed record InfeasibleOption(string OptionId, string Name, IReadOnlyList<string> Violations);

/// <summary>Thrown when a request cannot be evaluated; <see cref="Errors"/> maps field paths to messages.</summary>
public sealed class EvaluationValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The evaluation request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
