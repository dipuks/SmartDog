namespace SmartDog.Domain.Models;

public class DecisionCriterion
{
    public Guid Id { get; set; }

    public Guid DecisionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public Decision? Decision { get; set; }

    public ICollection<DecisionAlternativeCriterionRating> Ratings { get; set; }
        = new List<DecisionAlternativeCriterionRating>();
}
