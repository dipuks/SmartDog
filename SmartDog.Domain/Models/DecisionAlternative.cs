namespace SmartDog.Domain.Models;

public class DecisionAlternative
{
    public Guid Id { get; set; }

    public Guid DecisionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Score { get; set; }

    public Decision? Decision { get; set; }
}