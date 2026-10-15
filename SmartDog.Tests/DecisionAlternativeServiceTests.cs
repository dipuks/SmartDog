using Moq;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Tests;

public class DecisionAlternativeServiceTests
{
    [Fact]
    public async Task GetByDecisionIdAsync_MissingDecision_ReturnsNull()
    {
        var decisionId = Guid.NewGuid();
        var alternatives = new Mock<IDecisionAlternativeRepository>();
        var decisions = new Mock<IDecisionRepository>();
        decisions
            .Setup(repository => repository.ExistsAsync(decisionId, TestCurrentUser.OwnerId))
            .ReturnsAsync(false);
        var service = new DecisionAlternativeService(
            alternatives.Object,
            decisions.Object,
            TestCurrentUser.Owner);

        var result = await service.GetByDecisionIdAsync(decisionId);

        Assert.Null(result);
        alternatives.Verify(
            repository => repository.GetByDecisionIdAsync(decisionId),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MissingDecision_DoesNotCreateAlternative()
    {
        var decisionId = Guid.NewGuid();
        var alternatives = new Mock<IDecisionAlternativeRepository>();
        var decisions = new Mock<IDecisionRepository>();
        decisions
            .Setup(repository => repository.ExistsAsync(decisionId, TestCurrentUser.OwnerId))
            .ReturnsAsync(false);
        var service = new DecisionAlternativeService(
            alternatives.Object,
            decisions.Object,
            TestCurrentUser.Owner);

        var result = await service.CreateAsync(
            decisionId,
            new CreateDecisionAlternativeRequest("Option", null, 0));

        Assert.Null(result);
        alternatives.Verify(
            repository => repository.AddAsync(It.IsAny<DecisionAlternative>()),
            Times.Never);
        alternatives.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }
}
