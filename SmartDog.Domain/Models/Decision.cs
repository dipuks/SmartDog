namespace SmartDog.Domain.Models;

public class Decision
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public ICollection<DecisionAlternative> Alternatives { get; set; }
        = new List<DecisionAlternative>();

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}