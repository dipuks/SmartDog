using SmartDog.Domain.Models;

namespace SmartDog.Domain.Services;

public class DecisionQueryService
{
    private readonly List<Decision> _decisions =
    [
        new Decision
        {
            Id = Guid.NewGuid(),
            Title = "Choose Database",
            Description = "Select the database technology",
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        },

        new Decision
        {
            Id = Guid.NewGuid(),
            Title = "Choose Frontend",
            Description = "Select the frontend framework",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        },

        new Decision
        {
            Id = Guid.NewGuid(),
            Title = "Choose Cloud Platform",
            Description = "Select the cloud platform",
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }
    ];

    public IEnumerable<Decision> GetCompletedDecisions()
    {
        return _decisions
            .Where(d => d.Status == "Completed")
            .OrderByDescending(d => d.CreatedAt);
    }

    public Decision? GetById(Guid id)
    {
        return _decisions
            .FirstOrDefault(d => d.Id == id);
    }
}