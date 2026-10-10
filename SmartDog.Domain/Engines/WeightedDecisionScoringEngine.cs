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
}