using SmartDog.Domain.Models;

namespace SmartDog.Application.Services;

public class DecisionService
{
    public async Task<Decision> CreateDecisionAsync(
        string title,
        string? description)
    {
        await Task.Delay(100);

        return new Decision
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Status = "Draft"
        };
    }
}