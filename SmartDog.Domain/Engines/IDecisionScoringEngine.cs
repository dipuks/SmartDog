using SmartDog.Domain.Models;

namespace SmartDog.Domain.Engines;

public interface IDecisionScoringEngine
{
    IEnumerable<DecisionScore> Score(
        IEnumerable<DecisionAlternative> alternatives);

    IEnumerable<DecisionScore> Score(
        IEnumerable<DecisionCriterion> criteria,
        IEnumerable<DecisionAlternative> alternatives);
}