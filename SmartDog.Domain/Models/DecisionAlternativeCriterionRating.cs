namespace SmartDog.Domain.Models;

public class DecisionAlternativeCriterionRating
{
    public Guid DecisionId { get; set; }

    public Guid DecisionAlternativeId { get; set; }

    public Guid DecisionCriterionId { get; set; }

    public decimal Rating { get; set; }

    public DecisionAlternative? Alternative { get; set; }

    public DecisionCriterion? Criterion { get; set; }
}
