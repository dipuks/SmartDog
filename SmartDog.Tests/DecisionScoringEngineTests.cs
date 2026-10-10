using SmartDog.Domain.Engines;
using SmartDog.Domain.Models;

namespace SmartDog.Tests;

public class DecisionScoringEngineTests
{
    [Fact]
    public void Score_OrdersAlternativesByDescendingStoredScore()
    {
        var alternatives = new[]
        {
            new DecisionAlternative { Id = Guid.NewGuid(), Name = "Middle", Score = 50 },
            new DecisionAlternative { Id = Guid.NewGuid(), Name = "Highest", Score = 90 },
            new DecisionAlternative { Id = Guid.NewGuid(), Name = "Lowest", Score = 10 }
        };
        var engine = new WeightedDecisionScoringEngine();

        var results = engine.Score(alternatives).ToList();

        Assert.Equal(
            new[] { "Highest", "Middle", "Lowest" },
            results.Select(result => result.AlternativeName));
        Assert.Equal(
            new decimal[] { 90, 50, 10 },
            results.Select(result => result.Score));
    }

    [Fact]
    public void Score_EmptyAlternatives_ReturnsEmptyResults()
    {
        var engine = new WeightedDecisionScoringEngine();

        Assert.Empty(engine.Score(Array.Empty<DecisionAlternative>()));
    }

    [Fact]
    public void Score_WithCriteria_ReturnsWeightedDescendingResults()
    {
        var decisionId = Guid.NewGuid();
        var firstCriterionId = Guid.NewGuid();
        var secondCriterionId = Guid.NewGuid();
        var lowerAlternative = new DecisionAlternative
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            Name = "Lower"
        };
        lowerAlternative.CriterionRatings.Add(new DecisionAlternativeCriterionRating
        {
            DecisionId = decisionId,
            DecisionAlternativeId = lowerAlternative.Id,
            DecisionCriterionId = firstCriterionId,
            Rating = 100
        });
        lowerAlternative.CriterionRatings.Add(new DecisionAlternativeCriterionRating
        {
            DecisionId = decisionId,
            DecisionAlternativeId = lowerAlternative.Id,
            DecisionCriterionId = secondCriterionId,
            Rating = 0
        });

        var higherAlternative = new DecisionAlternative
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            Name = "Higher"
        };
        higherAlternative.CriterionRatings.Add(new DecisionAlternativeCriterionRating
        {
            DecisionId = decisionId,
            DecisionAlternativeId = higherAlternative.Id,
            DecisionCriterionId = firstCriterionId,
            Rating = 50
        });
        higherAlternative.CriterionRatings.Add(new DecisionAlternativeCriterionRating
        {
            DecisionId = decisionId,
            DecisionAlternativeId = higherAlternative.Id,
            DecisionCriterionId = secondCriterionId,
            Rating = 100
        });

        var criteria = new[]
        {
            new DecisionCriterion
            {
                Id = firstCriterionId,
                DecisionId = decisionId,
                Name = "Quality",
                Weight = 60
            },
            new DecisionCriterion
            {
                Id = secondCriterionId,
                DecisionId = decisionId,
                Name = "Cost",
                Weight = 40
            }
        };

        var results = new WeightedDecisionScoringEngine()
            .Score(criteria, [lowerAlternative, higherAlternative])
            .ToList();

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal(higherAlternative.Id, first.AlternativeId);
                Assert.Equal(70m, first.Score);
            },
            second =>
            {
                Assert.Equal(lowerAlternative.Id, second.AlternativeId);
                Assert.Equal(60m, second.Score);
            });
    }

    [Fact]
    public void Score_WeightsDoNotTotalOneHundred_Throws()
    {
        var decisionId = Guid.NewGuid();
        var criteria = new[]
        {
            new DecisionCriterion { Id = Guid.NewGuid(), DecisionId = decisionId, Weight = 70 }
        };

        Assert.Throws<InvalidScoringConfigurationException>(
            () => new WeightedDecisionScoringEngine().Score(
                criteria,
                Array.Empty<DecisionAlternative>()));
    }

    [Fact]
    public void Score_AlternativeMissingRating_Throws()
    {
        var decisionId = Guid.NewGuid();
        var alternative = new DecisionAlternative
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            Name = "Unrated"
        };
        var criteria = new[]
        {
            new DecisionCriterion { Id = Guid.NewGuid(), DecisionId = decisionId, Weight = 100 }
        };

        Assert.Throws<InvalidScoringConfigurationException>(
            () => new WeightedDecisionScoringEngine().Score(
                criteria,
                [alternative]));
    }
}
