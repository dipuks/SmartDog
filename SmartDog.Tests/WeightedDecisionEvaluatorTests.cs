using SmartDog.Domain.Evaluation;

namespace SmartDog.Tests;

public sealed class WeightedDecisionEvaluatorTests
{
    private readonly WeightedDecisionEvaluator _evaluator = new();

    private static EvaluationCriterion Criterion(string id, double weight, string? direction = null, string? name = null) =>
        new() { Id = id, Weight = weight, Direction = direction, Name = name };

    private static EvaluationOption Option(string id, params (string Criterion, double? Value)[] values) =>
        new() { Id = id, Values = values.ToDictionary(v => v.Criterion, v => v.Value) };

    /// <summary>impact (weight 3, maximize) and effort (weight 1, minimize); expected values worked out by hand.</summary>
    private static EvaluationRequest ImpactAndEffort() => new()
    {
        Question = "Which fix first?",
        Criteria = [Criterion("impact", 3, "maximize", "Impact"), Criterion("effort", 1, "minimize", "Effort")],
        Options =
        [
            Option("a", ("impact", 10), ("effort", 5)),
            Option("b", ("impact", 6), ("effort", 1)),
            Option("c", ("impact", 2), ("effort", 3))
        ]
    };

    [Fact]
    public void Ranks_by_weighted_sum_of_normalized_criteria()
    {
        var result = _evaluator.Evaluate(ImpactAndEffort());

        // impact 2..10 → a 1, b .5, c 0; effort 1..5 (minimize) → a 0, b 1, c .5; weights .75/.25.
        Assert.Equal(EvaluationStatus.Ranked, result.Status);
        Assert.Equal(["a", "b", "c"], result.Ranking.Select(r => r.OptionId));
        Assert.Equal([0.75, 0.625, 0.125], result.Ranking.Select(r => r.Score));
        Assert.Equal([1, 2, 3], result.Ranking.Select(r => r.Rank));
        var b = result.Ranking[1];
        Assert.Equal(new CriterionContribution("impact", 6, 0.5, 0.75, 0.375), b.Contributions[0]);
        Assert.Equal(new CriterionContribution("effort", 1, 1, 0.25, 0.25), b.Contributions[1]);
        Assert.Equal([0.75, 0.25], result.Criteria.Select(c => c.NormalizedWeight));
        Assert.Equal((2.0, 10.0), (result.Criteria[0].MinimumValue!.Value, result.Criteria[0].MaximumValue!.Value));
    }

    [Fact]
    public void Explanations_show_the_strongest_and_weakest_contributions()
    {
        var b = _evaluator.Evaluate(ImpactAndEffort()).Ranking[1];

        Assert.StartsWith("Rank 2 of 3 with score 0.625.", b.Explanation);
        Assert.Contains("Strongest: Impact 6 → 0.50 × weight 0.75 = 0.375", b.Explanation);
        Assert.Contains("Weakest: Effort 1 → 1.00 × weight 0.25 = 0.250", b.Explanation);
    }

    [Fact]
    public void Proportional_weights_give_the_same_ranking()
    {
        var doubled = ImpactAndEffort();
        doubled.Criteria![0].Weight = 6;
        doubled.Criteria[1].Weight = 2;

        Assert.Equal(_evaluator.Evaluate(ImpactAndEffort()).Ranking.Select(r => (r.OptionId, r.Score)),
            _evaluator.Evaluate(doubled).Ranking.Select(r => (r.OptionId, r.Score)));
    }

    [Fact]
    public void Hard_constraints_exclude_options_and_normalization_uses_feasible_options_only()
    {
        var request = ImpactAndEffort();
        request.Constraints = [new EvaluationConstraint { Id = "cap", CriterionId = "effort", Operator = "<=", Value = 3 }];

        var result = _evaluator.Evaluate(request);

        var excluded = Assert.Single(result.Infeasible);
        Assert.Equal("a", excluded.OptionId);
        Assert.Equal(["Effort = 5 violates <= 3 (constraint 'cap')."], excluded.Violations);
        Assert.Equal(["b", "c"], result.Ranking.Select(r => r.OptionId));
        Assert.Equal((2.0, 6.0), (result.Criteria[0].MinimumValue!.Value, result.Criteria[0].MaximumValue!.Value)); // a's 10 excluded
        Assert.Equal(1.0, result.Ranking[0].Score);
        Assert.Contains(result.Notes, n => n.StartsWith("1 of 3 option(s) violated a hard constraint"));
    }

    [Fact]
    public void No_feasible_option_is_reported_without_a_ranking()
    {
        var request = ImpactAndEffort();
        request.Constraints = [new EvaluationConstraint { CriterionId = "impact", Operator = ">", Value = 100 }];

        var result = _evaluator.Evaluate(request);

        Assert.Equal(EvaluationStatus.NoFeasibleOption, result.Status);
        Assert.Empty(result.Ranking);
        Assert.Equal(3, result.Infeasible.Count);
        Assert.Contains(result.Notes, n => n.Contains("No option satisfies every hard constraint"));
    }

    [Theory]
    [InlineData("<", 5, false)]
    [InlineData("<=", 5, true)]
    [InlineData(">", 4, true)]
    [InlineData(">=", 6, false)]
    [InlineData("==", 5, true)]
    [InlineData("!=", 5, false)]
    public void Every_constraint_operator_is_evaluated(string op, double threshold, bool feasible)
    {
        var request = new EvaluationRequest
        {
            Criteria = [Criterion("x", 1)],
            Options = [Option("only", ("x", 5))],
            Constraints = [new EvaluationConstraint { CriterionId = "x", Operator = op, Value = threshold }]
        };

        Assert.Equal(feasible ? EvaluationStatus.Ranked : EvaluationStatus.NoFeasibleOption, _evaluator.Evaluate(request).Status);
    }

    [Fact]
    public void Ties_are_ordered_by_option_id_and_reported()
    {
        var request = new EvaluationRequest
        {
            Criteria = [Criterion("impact", 1)],
            Options = [Option("zeta", ("impact", 5)), Option("alpha", ("impact", 5)), Option("mid", ("impact", 1))]
        };

        var result = _evaluator.Evaluate(request);

        Assert.Equal(["alpha", "zeta", "mid"], result.Ranking.Select(r => r.OptionId));
        Assert.Equal(["zeta"], result.Ranking[0].TiedWithOptionIds);
        Assert.Equal(["alpha"], result.Ranking[1].TiedWithOptionIds);
        Assert.Empty(result.Ranking[2].TiedWithOptionIds);
        Assert.Contains("Tied with zeta; ordered by option id.", result.Ranking[0].Explanation);
        Assert.Contains(result.Notes, n => n.StartsWith("1 group(s) of options have equal scores"));
    }

    [Fact]
    public void A_criterion_with_identical_values_is_reported_as_non_discriminating()
    {
        var request = ImpactAndEffort();
        foreach (var option in request.Options!) option.Values!["effort"] = 2;

        var result = _evaluator.Evaluate(request);

        Assert.Contains(result.Notes, n => n == "'Effort' has the same value (2) for every ranked option, so it does not affect the order.");
        Assert.All(result.Ranking, r => Assert.Equal(1.0, r.Contributions.Single(c => c.CriterionId == "effort").NormalizedValue));
        Assert.Equal(["a", "b", "c"], result.Ranking.Select(r => r.OptionId));
    }

    [Fact]
    public void A_single_option_scores_one()
    {
        var result = _evaluator.Evaluate(new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("only", ("x", 3))] });

        var only = Assert.Single(result.Ranking);
        Assert.Equal((1, 1.0), (only.Rank, only.Score));
        Assert.Empty(result.Notes);
    }

    [Fact]
    public void Evaluation_is_deterministic()
    {
        var first = _evaluator.Evaluate(ImpactAndEffort());
        var second = _evaluator.Evaluate(ImpactAndEffort());

        Assert.NotEqual(first.EvaluationId, second.EvaluationId);
        Assert.Equal(first.Ranking.Select(r => (r.OptionId, r.Score, r.Explanation)), second.Ranking.Select(r => (r.OptionId, r.Score, r.Explanation)));
    }

    [Fact]
    public void Negative_and_fractional_values_are_supported()
    {
        var result = _evaluator.Evaluate(new EvaluationRequest
        {
            Criteria = [Criterion("delta", 1)],
            Options = [Option("down", ("delta", -2.5)), Option("up", ("delta", 0.5))]
        });

        Assert.Equal(["up", "down"], result.Ranking.Select(r => r.OptionId));
    }

    public static TheoryData<EvaluationRequest, string> InvalidRequests => new()
    {
        { new EvaluationRequest { Options = [Option("a", ("x", 1))] }, "criteria" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)] }, "options" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1), Criterion("x", 2)], Options = [Option("a", ("x", 1))] }, "criteria[1].id" },
        { new EvaluationRequest { Criteria = [Criterion("bad id!", 1)], Options = [Option("a", ("bad id!", 1))] }, "criteria[0].id" },
        { new EvaluationRequest { Criteria = [Criterion("x", 0)], Options = [Option("a", ("x", 1))] }, "criteria[0].weight" },
        { new EvaluationRequest { Criteria = [Criterion("x", -1)], Options = [Option("a", ("x", 1))] }, "criteria[0].weight" },
        { new EvaluationRequest { Criteria = [Criterion("x", double.NaN)], Options = [Option("a", ("x", 1))] }, "criteria[0].weight" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1001)], Options = [Option("a", ("x", 1))] }, "criteria[0].weight" },
        { new EvaluationRequest { Criteria = [new EvaluationCriterion { Id = "x" }], Options = [Option("a", ("x", 1))] }, "criteria[0].weight" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1, "sideways")], Options = [Option("a", ("x", 1))] }, "criteria[0].direction" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1), Criterion("y", 1)], Options = [Option("a", ("x", 1))] }, "options[0].values" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", 1), ("z", 2))] }, "options[0].values.z" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", null))] }, "options[0].values.x" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", double.PositiveInfinity))] }, "options[0].values.x" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", 1)), Option("a", ("x", 2))] }, "options[1].id" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", 1))], Constraints = [new EvaluationConstraint { CriterionId = "nope", Operator = ">", Value = 1 }] }, "constraints[0].criterionId" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", 1))], Constraints = [new EvaluationConstraint { CriterionId = "x", Operator = "=>", Value = 1 }] }, "constraints[0].operator" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = [Option("a", ("x", 1))], Constraints = [new EvaluationConstraint { CriterionId = "x", Operator = ">" }] }, "constraints[0].value" },
        { new EvaluationRequest { Criteria = [Criterion("x", 1)], Options = Enumerable.Range(0, 201).Select(i => Option($"o{i}", ("x", i))).ToList() }, "options" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Invalid_requests_are_rejected_with_the_offending_field(EvaluationRequest request, string field)
    {
        var exception = Assert.Throws<EvaluationValidationException>(() => _evaluator.Evaluate(request));

        Assert.Contains(field, exception.Errors.Keys);
    }

    [Fact]
    public void All_problems_are_reported_together()
    {
        var request = new EvaluationRequest
        {
            Criteria = [Criterion("x", 0, "sideways")],
            Options = [Option("a", ("y", 1))]
        };

        var errors = Assert.Throws<EvaluationValidationException>(() => _evaluator.Evaluate(request)).Errors;

        Assert.Equal(["criteria[0].direction", "criteria[0].weight", "options[0].values", "options[0].values.y"], errors.Keys.Order());
    }
}
