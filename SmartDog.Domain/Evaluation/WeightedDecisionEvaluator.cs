using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartDog.Domain.Evaluation;

/// <summary>
/// Transparent multi-criteria ranking (weighted sum of min–max normalized criteria). Deterministic: the same request
/// always yields the same ranking. No machine learning or language model is involved.
/// <list type="number">
/// <item>Options violating any hard constraint are excluded and reported with the violated constraints.</item>
/// <item>Each criterion is normalized across the remaining options to 0–1 where 1 is best (max for "maximize", min
/// for "minimize"). A criterion whose remaining values are all equal normalizes to 1 for each, so it cannot change the
/// order. Values are equal when they differ by at most 1e-12 of their magnitude (floating-point noise), at any scale.</item>
/// <item>Weights are divided by their sum; score = Σ normalized weight × normalized value (0–1).</item>
/// <item>Higher score ranks first. Scores are compared at a resolution of 1e-9: options whose scores round to the same
/// multiple of 1e-9 are ties, ordered by option id (ordinal) and reported as such. The same rounded key drives both the
/// order and the ties, so tie groups are always adjacent and transitive.</item>
/// </list>
/// Constraint operators use the same equality, so <c>&lt;=</c> and <c>&gt;=</c> agree with <c>==</c>.
/// </summary>
public sealed partial class WeightedDecisionEvaluator
{
    public const int MaxCriteria = 20;
    public const int MaxOptions = 200;
    public const int MaxConstraints = 50;
    public const double MaxWeight = 1_000;
    /// <summary>Largest accepted magnitude for option and constraint values, so ranges cannot overflow.</summary>
    public const double MaxAbsoluteValue = 1e15;
    /// <summary>Scores (0–1) are ranked and tied at this resolution.</summary>
    private const double ScoreResolution = 1e-9;
    /// <summary>Values within this fraction of their magnitude are equal: floating-point noise, at any scale.</summary>
    private const double RelativeTolerance = 1e-12;

    public const string Method =
        "Weighted sum of min–max normalized criteria: each criterion is scaled to 0–1 across the feasible options " +
        "(1 = best), multiplied by its share of the total weight, and summed. Options violating a hard constraint are " +
        "excluded. Ties are ordered by option id.";

    public const string Limitations =
        "Scores are relative to the submitted options and weights (adding or removing an option can change them). " +
        "This is a transparent weighted ranking of the evidence provided, not a prediction or a machine-learning model.";

    [GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial Regex ValidId();

    private static readonly string[] Operators = ["<", "<=", ">", ">=", "==", "!="];

    public EvaluationResult Evaluate(EvaluationRequest request)
    {
        Validate(request);
        var criteria = request.Criteria!;
        var options = request.Options!;
        var constraints = request.Constraints ?? [];

        // 1. Hard constraints.
        var feasible = new List<EvaluationOption>();
        var infeasible = new List<InfeasibleOption>();
        foreach (var option in options)
        {
            var violations = constraints
                .Where(c => !Compare(Value(option, c.CriterionId!), c.Operator!, c.Value!.Value))
                .Select(c => $"{CriterionName(criteria, c.CriterionId!)} = {Format(Value(option, c.CriterionId!))} violates " +
                             $"{c.Operator} {Format(c.Value!.Value)}{(string.IsNullOrWhiteSpace(c.Id) ? "" : $" (constraint '{c.Id}')")}.")
                .ToList();
            if (violations.Count > 0) infeasible.Add(new InfeasibleOption(option.Id!, OptionName(option), violations));
            else feasible.Add(option);
        }

        // 2. Normalized weights and observed ranges over feasible options.
        var totalWeight = criteria.Sum(c => c.Weight!.Value);
        var weighted = criteria.Select(c =>
        {
            var values = feasible.Select(o => Value(o, c.Id!)).ToList();
            return new WeightedCriterion(
                c.Id!, CriterionName(criteria, c.Id!), DirectionOf(c), c.Weight!.Value, Round(c.Weight.Value / totalWeight),
                values.Count == 0 ? null : values.Min(), values.Count == 0 ? null : values.Max());
        }).ToList();

        var notes = new List<string>();
        if (infeasible.Count > 0)
            notes.Add($"{infeasible.Count} of {options.Count} option(s) violated a hard constraint and were not ranked.");
        if (feasible.Count == 0)
        {
            notes.Add("No option satisfies every hard constraint; relax a constraint or add options.");
            return new EvaluationResult(Guid.NewGuid(), EvaluationStatus.NoFeasibleOption, request.Question, Method, weighted, [], infeasible, notes, Limitations);
        }
        foreach (var c in weighted.Where(c => feasible.Count > 1 && NearlyEqual(c.MinimumValue!.Value, c.MaximumValue!.Value)))
            notes.Add($"'{c.Name}' has the same value ({Format(c.MinimumValue!.Value)}) for every ranked option, so it does not affect the order.");

        // 3. Scores.
        var scored = feasible.Select(option =>
        {
            var contributions = weighted.Select(c =>
            {
                var raw = Value(option, c.Id);
                var range = c.MaximumValue!.Value - c.MinimumValue!.Value;
                var normalized = NearlyEqual(c.MinimumValue.Value, c.MaximumValue.Value) ? 1.0
                    : c.Direction == "minimize" ? (c.MaximumValue.Value - raw) / range
                    : (raw - c.MinimumValue.Value) / range;
                var share = c.Weight / totalWeight;
                return (Raw: raw, Normalized: normalized, Share: share, Contribution: share * normalized, Criterion: c);
            }).ToList();
            var score = contributions.Sum(x => x.Contribution);
            return (Option: option, Score: score, Key: ScoreKey(score), Contributions: contributions);
        })
        // One quantized key orders the ranking and defines ties, so floating-point noise can never override the
        // documented id tie-break and tied options are always adjacent.
        .OrderByDescending(x => x.Key)
        .ThenBy(x => x.Option.Id, StringComparer.Ordinal)
        .ToList();

        // 4. Ranking, ties and explanations.
        var ranking = new List<RankedOption>();
        for (var i = 0; i < scored.Count; i++)
        {
            var current = scored[i];
            var tiedWith = scored.Where(x => x.Option.Id != current.Option.Id && x.Key == current.Key)
                .Select(x => x.Option.Id!).ToList();
            var contributions = current.Contributions.Select(x => new CriterionContribution(
                x.Criterion.Id, x.Raw, Round(x.Normalized), Round(x.Share), Round(x.Contribution))).ToList();
            // Tied options report their shared key as the score, so a response never shows a tie with unequal scores.
            var score = tiedWith.Count == 0 ? Round(current.Score) : Round(current.Key * ScoreResolution);
            ranking.Add(new RankedOption(i + 1, current.Option.Id!, OptionName(current.Option), score, contributions,
                Explain(i + 1, scored.Count, current.Score, current.Contributions.Select(x => (x.Criterion, x.Raw, x.Normalized, x.Contribution)).ToList(), tiedWith),
                tiedWith));
        }
        var tieGroups = scored.GroupBy(x => x.Key).Count(g => g.Count() > 1);
        if (tieGroups > 0) notes.Add($"{tieGroups} group(s) of options have equal scores; tied options are ordered by option id.");

        return new EvaluationResult(Guid.NewGuid(), EvaluationStatus.Ranked, request.Question, Method, weighted, ranking, infeasible, notes, Limitations);
    }

    /// <summary>Throws <see cref="EvaluationValidationException"/> listing every problem with the request.</summary>
    public static void Validate(EvaluationRequest? request)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list)) errors[field] = list = [];
            list.Add(message);
        }

        if (request is null) throw new EvaluationValidationException(new Dictionary<string, string[]> { ["request"] = ["A request body is required."] });
        if (request.Question?.Length > 500) Add("question", "Question must not exceed 500 characters.");

        var criteria = request.Criteria ?? [];
        var criterionIds = new HashSet<string>(StringComparer.Ordinal);
        if (criteria.Count == 0) Add("criteria", "At least one criterion is required.");
        if (criteria.Count > MaxCriteria) Add("criteria", $"At most {MaxCriteria} criteria are allowed.");
        for (var i = 0; i < criteria.Count; i++)
        {
            var c = criteria[i];
            var path = $"criteria[{i}]";
            if (c is null) { Add(path, "Criterion must not be null."); continue; }
            if (c.Id is null || !ValidId().IsMatch(c.Id)) Add($"{path}.id", "Id is required: 1–64 letters, digits, '.', '_' or '-'.");
            else if (!criterionIds.Add(c.Id)) Add($"{path}.id", $"Duplicate criterion id '{c.Id}'.");
            if (c.Name?.Length > 200) Add($"{path}.name", "Name must not exceed 200 characters.");
            if (c.Weight is null) Add($"{path}.weight", "Weight is required.");
            else if (!double.IsFinite(c.Weight.Value) || c.Weight <= 0 || c.Weight > MaxWeight)
                Add($"{path}.weight", $"Weight must be greater than 0 and at most {MaxWeight:0}.");
            if (c.Direction is not null && !c.Direction.Equals("maximize", StringComparison.OrdinalIgnoreCase) && !c.Direction.Equals("minimize", StringComparison.OrdinalIgnoreCase))
                Add($"{path}.direction", "Direction must be 'maximize' or 'minimize'.");
        }

        var options = request.Options ?? [];
        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        if (options.Count == 0) Add("options", "At least one option is required.");
        if (options.Count > MaxOptions) Add("options", $"At most {MaxOptions} options are allowed.");
        for (var i = 0; i < options.Count; i++)
        {
            var o = options[i];
            var path = $"options[{i}]";
            if (o is null) { Add(path, "Option must not be null."); continue; }
            if (o.Id is null || !ValidId().IsMatch(o.Id)) Add($"{path}.id", "Id is required: 1–64 letters, digits, '.', '_' or '-'.");
            else if (!optionIds.Add(o.Id)) Add($"{path}.id", $"Duplicate option id '{o.Id}'.");
            if (o.Name?.Length > 200) Add($"{path}.name", "Name must not exceed 200 characters.");
            var values = o.Values ?? [];
            foreach (var id in criterionIds.Where(id => !values.ContainsKey(id)))
                Add($"{path}.values", $"Missing a value for criterion '{id}'.");
            foreach (var (key, value) in values)
            {
                if (!criterionIds.Contains(key)) Add($"{path}.values.{key}", $"'{key}' is not one of the request's criteria.");
                else if (value is null || !IsAcceptedValue(value.Value)) Add($"{path}.values.{key}", ValueRule);
            }
        }

        var constraints = request.Constraints ?? [];
        if (constraints.Count > MaxConstraints) Add("constraints", $"At most {MaxConstraints} constraints are allowed.");
        for (var i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            var path = $"constraints[{i}]";
            if (c is null) { Add(path, "Constraint must not be null."); continue; }
            if (c.CriterionId is null || !criterionIds.Contains(c.CriterionId)) Add($"{path}.criterionId", "Constraint must reference one of the request's criteria.");
            if (c.Operator is null || !Operators.Contains(c.Operator)) Add($"{path}.operator", "Operator must be one of <, <=, >, >=, ==, !=.");
            if (c.Value is null || !IsAcceptedValue(c.Value.Value)) Add($"{path}.value", ValueRule);
        }

        if (errors.Count > 0) throw new EvaluationValidationException(errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));
    }

    private static string Explain(int rank, int count, double score,
        IReadOnlyList<(WeightedCriterion Criterion, double Raw, double Normalized, double Contribution)> parts, IReadOnlyList<string> tiedWith)
    {
        var ordered = parts.OrderByDescending(p => p.Contribution).ThenBy(p => p.Criterion.Id, StringComparer.Ordinal).ToList();
        string Describe((WeightedCriterion Criterion, double Raw, double Normalized, double Contribution) p) =>
            $"{p.Criterion.Name} {Format(p.Raw)} → {p.Normalized:0.00} × weight {p.Criterion.NormalizedWeight:0.00} = {p.Contribution:0.000}";
        var text = $"Rank {rank} of {count} with score {score:0.000}. Strongest: {Describe(ordered[0])}.";
        if (ordered.Count > 1) text += $" Weakest: {Describe(ordered[^1])}.";
        if (tiedWith.Count > 0) text += $" Tied with {string.Join(", ", tiedWith)}; ordered by option id.";
        return text;
    }

    private static double Value(EvaluationOption option, string criterionId) => option.Values![criterionId]!.Value;

    private static string DirectionOf(EvaluationCriterion c) =>
        c.Direction?.Equals("minimize", StringComparison.OrdinalIgnoreCase) == true ? "minimize" : "maximize";

    private static string CriterionName(IEnumerable<EvaluationCriterion> criteria, string id) =>
        criteria.First(c => c.Id == id) is { Name: { Length: > 0 } name } ? name : id;

    private static string OptionName(EvaluationOption option) => string.IsNullOrWhiteSpace(option.Name) ? option.Id! : option.Name;

    private static bool Compare(double actual, string op, double expected) => op switch
    {
        "<" => actual < expected && !NearlyEqual(actual, expected),
        "<=" => actual < expected || NearlyEqual(actual, expected),
        ">" => actual > expected && !NearlyEqual(actual, expected),
        ">=" => actual > expected || NearlyEqual(actual, expected),
        "==" => NearlyEqual(actual, expected),
        "!=" => !NearlyEqual(actual, expected),
        _ => false
    };

    private const string ValueRule = "Value must be a finite number between -1e15 and 1e15.";

    private static bool IsAcceptedValue(double value) => double.IsFinite(value) && Math.Abs(value) <= MaxAbsoluteValue;

    /// <summary>Equal, or different only by floating-point noise relative to their magnitude.</summary>
    internal static bool NearlyEqual(double a, double b) =>
        a == b || Math.Abs(a - b) <= RelativeTolerance * Math.Max(Math.Abs(a), Math.Abs(b));

    /// <summary>The score as a whole number of <see cref="ScoreResolution"/> steps: the ranking and tie key.</summary>
    internal static long ScoreKey(double score) => (long)Math.Round(score / ScoreResolution, MidpointRounding.AwayFromZero);

    private static string Format(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static double Round(double value) => Math.Round(value, 6);
}
