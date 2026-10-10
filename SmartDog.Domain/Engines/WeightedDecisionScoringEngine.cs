using SmartDog.Domain.Models;

namespace SmartDog.Domain.Engines;

public class WeightedDecisionScoringEngine : IDecisionScoringEngine
{
    public IEnumerable<DecisionScore> Score(
        IEnumerable<DecisionAlternative> alternatives)
    {
        return alternatives
            .Select(alternative => new DecisionScore(
                alternative.Id,
                alternative.Name,
                alternative.Score))
            .OrderByDescending(result => result.Score);
    }

    public IEnumerable<DecisionScore> Score(
        IEnumerable<DecisionCriterion> criteria,
        IEnumerable<DecisionAlternative> alternatives)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(alternatives);

        var decisionCriteria = criteria.ToList();
        var decisionAlternatives = alternatives.ToList();

        if (decisionCriteria.Count == 0)
        {
            throw new InvalidScoringConfigurationException(
                "At least one criterion is required to calculate weighted results.");
        }

        if (decisionCriteria.Any(criterion =>
                criterion.Weight < 0 || criterion.Weight > 100) ||
            decisionCriteria.Sum(criterion => criterion.Weight) != 100m)
        {
            throw new InvalidScoringConfigurationException(
                "Criterion weights must be between 0 and 100 and total exactly 100.");
        }

        var decisionId = decisionCriteria[0].DecisionId;

        if (decisionCriteria.Any(criterion => criterion.DecisionId != decisionId) ||
            decisionAlternatives.Any(alternative => alternative.DecisionId != decisionId))
        {
            throw new InvalidScoringConfigurationException(
                "Criteria and alternatives must belong to the same decision.");
        }

        var criterionIds = decisionCriteria
            .Select(criterion => criterion.Id)
            .ToHashSet();

        if (criterionIds.Count != decisionCriteria.Count)
        {
            throw new InvalidScoringConfigurationException(
                "Criterion identifiers must be unique.");
        }

        var results = decisionAlternatives.Select(alternative =>
        {
            var ratings = alternative.CriterionRatings.ToList();

            if (ratings.Count != criterionIds.Count ||
                ratings.Any(rating =>
                    rating.DecisionId != decisionId ||
                    rating.DecisionAlternativeId != alternative.Id ||
                    !criterionIds.Contains(rating.DecisionCriterionId) ||
                    rating.Rating < 0 || rating.Rating > 100) ||
                ratings.Select(rating => rating.DecisionCriterionId).Distinct().Count() != criterionIds.Count)
            {
                throw new InvalidScoringConfigurationException(
                    "Every alternative must have exactly one rating from 0 to 100 for each criterion.");
            }

            var ratingsByCriterion = ratings.ToDictionary(
                rating => rating.DecisionCriterionId,
                rating => rating.Rating);

            var score = decisionCriteria.Sum(criterion =>
                criterion.Weight * ratingsByCriterion[criterion.Id] / 100m);

            return new DecisionScore(
                alternative.Id,
                alternative.Name,
                Math.Round(score, 2, MidpointRounding.AwayFromZero));
        });

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.AlternativeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.AlternativeId)
            .ToList();
    }
}