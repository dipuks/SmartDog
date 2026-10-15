using Castle.Core.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;

namespace SmartDog.Tests;

public class DecisionCommandServiceTests
{
    private readonly IMemoryCache _cache = new MemoryCache(
    new MemoryCacheOptions());
    [Fact]
    public async Task CreateAsync_ShouldCreateDecision()
    {
        // Arrange

        var repository = new Mock<IDecisionRepository>();

        var logger = new Mock<
            ILogger<DecisionCommandService>>();

        var service = new DecisionCommandService(
            repository.Object,
            logger.Object,
            _cache,
            TestCurrentUser.Owner);

        var request = new CreateDecisionRequest(
            "Choose AI Model",
            "Select the AI model for SmartDog");

        // Act

        var result = await service.CreateAsync(request);

        // Assert

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Choose AI Model", result.Title);
        Assert.Equal(
            "Select the AI model for SmartDog",
            result.Description);
        Assert.Equal("Created", result.Status);

        repository.Verify(
            r => r.AddAsync(It.IsAny<Decision>()),
            Times.Once);

        repository.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenDecisionDoesNotExist()
    {
        // Arrange

        var repository = new Mock<IDecisionRepository>();

        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), TestCurrentUser.OwnerId))
            .ReturnsAsync((Decision?)null);

        var logger = new Mock<
            ILogger<DecisionCommandService>>();

        var service = new DecisionCommandService(
            repository.Object,
            logger.Object,
            _cache,
            TestCurrentUser.Owner);

        var id = Guid.NewGuid();

        var request = new UpdateDecisionRequest(
            "Updated Decision",
            "Updated description",
            "Completed",
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act

        var result = await service.UpdateAsync(id, request);

        // Assert

        Assert.Null(result);

        repository.Verify(
            r => r.Update(It.IsAny<Decision>()),
            Times.Never);

        repository.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDecision()
    {
        // Arrange

        var id = Guid.NewGuid();

        var existingDecision = new Decision
        {
            Id = id,
            Title = "Old Title",
            Description = "Old description",
            CreatedAt = DateTime.UtcNow,
            Status = "Created"
        };

        var repository = new Mock<IDecisionRepository>();

        repository
            .Setup(r => r.GetByIdAsync(id, TestCurrentUser.OwnerId))
            .ReturnsAsync(existingDecision);

        var logger = new Mock<
            ILogger<DecisionCommandService>>();

        var service = new DecisionCommandService(
            repository.Object,
            logger.Object,
            _cache,
            TestCurrentUser.Owner);

        var request = new UpdateDecisionRequest(
            "New Title",
            "New description",
            "Completed",
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act

        var result = await service.UpdateAsync(
            id,
            request);

        // Assert

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal(
            "New description",
            result.Description);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            result.RowVersion);

        repository.Verify(
            r => r.Update(existingDecision),
            Times.Once);

        repository.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidStatusTransition()
    {
        var id = Guid.NewGuid();
        var decision = new Decision
        {
            Id = id,
            Title = "Completed decision",
            Status = DecisionStatuses.Completed
        };
        var repository = new Mock<IDecisionRepository>();
        repository
            .Setup(r => r.GetByIdAsync(id, TestCurrentUser.OwnerId))
            .ReturnsAsync(decision);
        repository
            .Setup(r => r.DeleteAsync(decision))
            .Returns(Task.CompletedTask);
        var service = new DecisionCommandService(
            repository.Object,
            Mock.Of<ILogger<DecisionCommandService>>(),
            _cache,
            TestCurrentUser.Owner);
        var request = new UpdateDecisionRequest(
            "Reopen decision",
            null,
            DecisionStatuses.InProgress,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        await Assert.ThrowsAsync<DecisionStatusTransitionException>(
            () => service.UpdateAsync(id, request));

        repository.Verify(r => r.Update(It.IsAny<Decision>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenDecisionDoesNotExist()
    {
        // Arrange

        var id = Guid.NewGuid();

        var repository = new Mock<IDecisionRepository>();

        repository
            .Setup(r => r.GetByIdAsync(id, TestCurrentUser.OwnerId))
            .ReturnsAsync((Decision?)null);
        repository
            .Setup(r => r.DeleteAsync(It.IsAny<Decision>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<
            ILogger<DecisionCommandService>>();

        var service = new DecisionCommandService(
            repository.Object,
            logger.Object,
            _cache,
            TestCurrentUser.Owner);

        // Act

        var result = await service.DeleteAsync(id);

        // Assert

        Assert.False(result);

        repository.Verify(
            r => r.DeleteAsync(It.IsAny<Decision>()),
            Times.Never);

        repository.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteDecision()
    {
        // Arrange

        var id = Guid.NewGuid();

        var decision = new Decision
        {
            Id = id,
            Title = "Delete Me",
            Description = "Test decision",
            CreatedAt = DateTime.UtcNow,
            Status = "Created"
        };

        var repository = new Mock<IDecisionRepository>();

        repository
            .Setup(r => r.GetByIdAsync(id, TestCurrentUser.OwnerId))
            .ReturnsAsync(decision);

        var logger = new Mock<
            ILogger<DecisionCommandService>>();

        var service = new DecisionCommandService(
            repository.Object,
            logger.Object,
            _cache,
            TestCurrentUser.Owner);

        // Act

        var result = await service.DeleteAsync(id);

        // Assert

        Assert.True(result);

        repository.Verify(
            r => r.DeleteAsync(decision),
            Times.Once);

        repository.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CreateAsync_AssignsTheSignedInUserAsOwner()
    {
        var repository = new Mock<IDecisionRepository>();
        Decision? added = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<Decision>()))
            .Callback<Decision>(decision => added = decision)
            .Returns(Task.CompletedTask);
        var service = new DecisionCommandService(
            repository.Object,
            Mock.Of<ILogger<DecisionCommandService>>(),
            _cache,
            TestCurrentUser.Owner);

        await service.CreateAsync(new CreateDecisionRequest("Pick a vendor", null));

        Assert.Equal(TestCurrentUser.OwnerId, added?.OwnerId);
    }

    [Fact]
    public async Task RequestsWithoutAUserId_AreRefusedBeforeAnyLookup()
    {
        // A null owner must never reach a query: it would match decisions that have no owner.
        var repository = new Mock<IDecisionRepository>(MockBehavior.Strict);
        var service = new DecisionCommandService(
            repository.Object,
            Mock.Of<ILogger<DecisionCommandService>>(),
            _cache,
            new TestCurrentUser(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(new CreateDecisionRequest("Pick a vendor", null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeleteAsync(Guid.NewGuid()));
    }
}
