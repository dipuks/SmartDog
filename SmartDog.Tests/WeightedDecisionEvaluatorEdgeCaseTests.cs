using SmartDog.Domain.Evaluation;

namespace SmartDog.Tests;

/// <summary>
/// Rounding, tie and scale edge cases of <see cref="WeightedDecisionEvaluator"/>, and a comparison with an independent
/// reference implementation (decimal arithmetic) over seeded random requests.
/// </summary>
public sealed class WeightedDecisionEvaluatorEdgeCaseTests
{
    private readonly WeightedDecisionEvaluator _evaluator = new();

    private static EvaluationCriterion Criterion(string id, double weight, string? direction = null) =>
        new() { Id = id, Weight = weight, Direction = direction };

    private static EvaluationOption Option(string id, params (string Criterion, double Value)[] values) =>
        new() { Id = id, Values = values.ToDictionary(v => v.Criterion, v => (double?)v.Value) };

    [Fact]
    public void Scores_closer_than_the_resolution_but_on_different_steps_are_not_tied_and_rank_by_score()
    {
        // Normalized scores 0.5, 0.5 + 0.8e-9 and 0.5 + 1.6e-9. A pairwise ±1e-9 tolerance made a~b and b~c but not a~c.
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("x", 1)],
            Options =
            [
                Option("a", ("x", 5e8)), Option("b", ("x", 5e8 + 0.8)), Option("c", ("x", 5e8 + 1.6)),
                Option("min", ("x", 0)), Option("max", ("x", 1e9))
            ]
        });

        Assert.Equal(["max", "c", "b", "a", "min"], result.Ranking.Select(r => r.OptionId));
        Assert.All(result.Ranking, r => Assert.Empty(r.TiedWithOptionIds));
        Assert.DoesNotContain(result.Notes, n => n.Contains("equal scores"));
    }

    [Fact]
    public void Floating_point_noise_in_scores_is_a_tie_ordered_by_id()
    {
        // 0.1/0.6 + 0.2/0.6 and 0.3/0.6 are both 0.5 mathematically but differ in the last bit as doubles.
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("p", 0.1), Criterion("q", 0.2), Criterion("r", 0.3)],
            Options = [Option("zz", ("p", 1), ("q", 1), ("r", 0)), Option("aa", ("p", 0), ("q", 0), ("r", 1))]
        });

        Assert.Equal(["aa", "zz"], result.Ranking.Select(r => r.OptionId));
        Assert.Equal(["zz"], result.Ranking[0].TiedWithOptionIds);
        Assert.Equal(result.Ranking[0].Score, result.Ranking[1].Score);
    }

    [Fact]
    public void Tiny_values_that_differ_still_discriminate()
    {
        // Differences of 1e-10 used to fall under an absolute 1e-9 tolerance, so the criterion was ignored.
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("risk", 1, "minimize")],
            Options = [Option("a", ("risk", 3e-10)), Option("b", ("risk", 1e-10)), Option("c", ("risk", 5e-10))]
        });

        Assert.Equal(["b", "a", "c"], result.Ranking.Select(r => r.OptionId));
        Assert.Equal([1.0, 0.5, 0.0], result.Ranking.Select(r => r.Score));
        Assert.DoesNotContain(result.Notes, n => n.Contains("same value"));
    }

    [Fact]
    public void Huge_values_that_differ_only_by_noise_count_as_equal()
    {
        // 1e12 and its next representable neighbours: a relative difference of about 1e-16, which is noise.
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("cost", 1, "minimize"), Criterion("value", 1)],
            Options =
            [
                Option("a", ("cost", 1e12), ("value", 1)),
                Option("b", ("cost", Math.BitIncrement(1e12)), ("value", 2))
            ]
        });

        Assert.Equal(["b", "a"], result.Ranking.Select(r => r.OptionId)); // only "value" decides
        Assert.Contains(result.Notes, n => n.StartsWith("'cost' has the same value"));
    }

    [Theory]
    [InlineData("<=", true)]
    [InlineData(">=", true)]
    [InlineData("==", true)]
    [InlineData("<", false)]
    [InlineData(">", false)]
    [InlineData("!=", false)]
    public void Inclusive_operators_agree_with_equality(string op, bool feasible)
    {
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("x", 1)],
            Options = [Option("only", ("x", 0.1 + 0.2))], // 0.30000000000000004
            Constraints = [new EvaluationConstraint { CriterionId = "x", Operator = op, Value = 0.3 }]
        });

        Assert.Equal(feasible ? EvaluationStatus.Ranked : EvaluationStatus.NoFeasibleOption, result.Status);
    }

    [Fact]
    public void Values_beyond_the_accepted_magnitude_are_rejected()
    {
        var error = Assert.Throws<EvaluationValidationException>(() => _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("x", 1)],
            Options = [Option("a", ("x", 1e308)), Option("b", ("x", -1e308))],
            Constraints = [new EvaluationConstraint { CriterionId = "x", Operator = "<", Value = 2e15 }]
        }));

        Assert.Contains("options[0].values.x", error.Errors.Keys);
        Assert.Contains("options[1].values.x", error.Errors.Keys);
        Assert.Contains("constraints[0].value", error.Errors.Keys);
    }

    /// <summary>
    /// 400 seeded random requests on integer grids (so exact ties exist and every other score difference is far above
    /// the 1e-9 resolution) compared with a reference that computes every score in decimal and ranks by it.
    /// </summary>
    [Fact]
    public void Matches_an_independent_decimal_reference_on_seeded_random_requests()
    {
        var random = new Random(20261015);
        string[] operators = ["<", "<=", ">", ">=", "==", "!="];
        for (var run = 0; run < 400; run++)
        {
            var criteria = Enumerable.Range(0, random.Next(1, 5))
                .Select(i => Criterion($"c{i}", random.Next(1, 11), random.Next(2) == 0 ? "maximize" : "minimize")).ToList();
            var options = Enumerable.Range(0, random.Next(1, 9))
                .Select(i => new EvaluationOption
                {
                    Id = $"o{random.Next(0, 1000):D3}-{i}",
                    Values = criteria.ToDictionary(c => c.Id!, _ => (double?)random.Next(0, 6))
                }).ToList();
            var constraints = random.Next(3) == 0
                ? [new EvaluationConstraint { CriterionId = criteria[0].Id, Operator = operators[random.Next(6)], Value = random.Next(0, 6) }]
                : new List<EvaluationConstraint>();
            var request = new EvaluationRequest { Criteria = criteria, Options = options, Constraints = constraints };

            var actual = _evaluator.Evaluate(request);
            var expected = Reference(request);

            Assert.Equal(expected.Select(e => e.Id), actual.Ranking.Select(r => r.OptionId));
            for (var i = 0; i < expected.Count; i++)
            {
                // Reported scores are rounded to 6 decimals.
                Assert.InRange(Math.Abs(actual.Ranking[i].Score - (double)expected[i].Score), 0, 5.1e-7);
                Assert.Equal(expected.Where(e => e.Id != expected[i].Id && e.Score == expected[i].Score).Select(e => e.Id).Order(StringComparer.Ordinal),
                    actual.Ranking[i].TiedWithOptionIds.Order(StringComparer.Ordinal));
            }
        }
    }

    /// <summary>
    /// Weighted sum of min–max normalized values, computed in decimal from the documented definition. Scores are compared
    /// at 20 decimals, far finer than the evaluator's 1e-9 but above decimal rounding noise.
    /// </summary>
    private static List<(string Id, decimal Score)> Reference(EvaluationRequest request)
    {
        static bool Holds(decimal value, string op, decimal threshold) => op switch
        {
            "<" => value < threshold, "<=" => value <= threshold, ">" => value > threshold,
            ">=" => value >= threshold, "==" => value == threshold, _ => value != threshold
        };
        var feasible = request.Options!.Where(o => (request.Constraints ?? []).All(c =>
            Holds((decimal)o.Values![c.CriterionId!]!.Value, c.Operator!, (decimal)c.Value!.Value))).ToList();
        var totalWeight = request.Criteria!.Sum(c => (decimal)c.Weight!.Value);
        return feasible
            .Select(o => (Id: o.Id!, Score: request.Criteria!.Sum(c =>
            {
                var values = feasible.Select(f => (decimal)f.Values![c.Id!]!.Value).ToList();
                decimal min = values.Min(), max = values.Max(), value = (decimal)o.Values![c.Id!]!.Value;
                var normalized = max == min ? 1m : c.Direction == "minimize" ? (max - value) / (max - min) : (value - min) / (max - min);
                return (decimal)c.Weight!.Value / totalWeight * normalized;
            })))
            .Select(o => (o.Id, Score: Math.Round(o.Score, 20)))
            .OrderByDescending(o => o.Score).ThenBy(o => o.Id, StringComparer.Ordinal)
            .ToList();
    }
}
