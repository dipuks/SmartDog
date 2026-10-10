using SmartDog.Application.DTOs;

namespace SmartDog.Application.Services;

public interface IDecisionCommandService
{
    Task<DecisionDto> CreateAsync(CreateDecisionRequest request);

    Task<DecisionDto?> UpdateAsync(
        Guid id,
        UpdateDecisionRequest request);

    Task<bool> DeleteAsync(Guid id);
}