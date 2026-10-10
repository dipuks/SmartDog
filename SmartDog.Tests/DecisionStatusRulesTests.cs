using SmartDog.Domain.Models;

namespace SmartDog.Tests;

public class DecisionStatusRulesTests
{
    [Theory]
    [InlineData(DecisionStatuses.Draft, DecisionStatuses.Created)]
    [InlineData(DecisionStatuses.Draft, DecisionStatuses.Cancelled)]
    [InlineData(DecisionStatuses.Created, DecisionStatuses.InProgress)]
    [InlineData(DecisionStatuses.Created, DecisionStatuses.Completed)]
    [InlineData(DecisionStatuses.Created, DecisionStatuses.Cancelled)]
    [InlineData(DecisionStatuses.InProgress, DecisionStatuses.Completed)]
    [InlineData(DecisionStatuses.InProgress, DecisionStatuses.Cancelled)]
    public void CanTransition_AllowedTransition_ReturnsTrue(
        string currentStatus,
        string requestedStatus)
    {
        Assert.True(
            DecisionStatuses.CanTransition(currentStatus, requestedStatus));
    }

    [Theory]
    [InlineData(DecisionStatuses.Completed, DecisionStatuses.InProgress)]
    [InlineData(DecisionStatuses.Cancelled, DecisionStatuses.Created)]
    [InlineData(DecisionStatuses.Draft, DecisionStatuses.Completed)]
    [InlineData("Unknown", DecisionStatuses.Created)]
    public void CanTransition_DisallowedTransition_ReturnsFalse(
        string currentStatus,
        string requestedStatus)
    {
        Assert.False(
            DecisionStatuses.CanTransition(currentStatus, requestedStatus));
    }

    [Theory]
    [InlineData(DecisionStatuses.Draft)]
    [InlineData(DecisionStatuses.Created)]
    [InlineData(DecisionStatuses.InProgress)]
    [InlineData(DecisionStatuses.Completed)]
    [InlineData(DecisionStatuses.Cancelled)]
    public void IsKnown_SupportedStatus_ReturnsTrue(string status)
    {
        Assert.True(DecisionStatuses.IsKnown(status));
    }
}