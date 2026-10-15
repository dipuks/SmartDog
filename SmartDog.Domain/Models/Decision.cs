namespace SmartDog.Domain.Models;

public class Decision
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The user who created the decision. Only that user can read or change it, its criteria, alternatives, ratings,
    /// results and assistant answers. Null only for decisions created before ownership existed: those are visible to
    /// nobody until an owner is assigned (see the README).
    /// </summary>
    public Guid? OwnerId { get; set; }

    public ICollection<DecisionAlternative> Alternatives { get; set; }
        = new List<DecisionAlternative>();

    public ICollection<DecisionCriterion> Criteria { get; set; }
        = new List<DecisionCriterion>();

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}