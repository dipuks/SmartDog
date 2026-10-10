namespace SmartDog.Domain.Models;

public sealed class DecisionStatusTransitionException : InvalidOperationException
{
    public DecisionStatusTransitionException(
        string currentStatus,
        string requestedStatus)
        : base($"Cannot transition a decision from '{currentStatus}' to '{requestedStatus}'.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public string CurrentStatus { get; }

    public string RequestedStatus { get; }
}
