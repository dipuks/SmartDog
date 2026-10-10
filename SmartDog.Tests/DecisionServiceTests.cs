using SmartDog.Application.Services;

namespace SmartDog.Tests;

public class DecisionServiceTests
{
    [Fact]
    public async Task CreateDecisionAsync_ShouldCreateDraftDecision()
    {
        // Arrange
        var service = new DecisionService();

        // Act
        var result = await service.CreateDecisionAsync(
            "Choose Database",
            "Select the database technology");

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Choose Database", result.Title);
        Assert.Equal(
            "Select the database technology",
            result.Description);

        Assert.Equal("Draft", result.Status);
    }
}