namespace SmartDog.Domain.Models;

public static class DecisionStatuses
{
    public const string Draft = "Draft";
    public const string Created = "Created";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static bool IsKnown(string? status)
    {
        return status is Draft or Created or InProgress or Completed or Cancelled;
    }

    public static bool CanTransition(string currentStatus, string requestedStatus)
    {
        if (currentStatus == requestedStatus)
        {
            return IsKnown(currentStatus);
        }

        return currentStatus switch
        {
            Draft => requestedStatus is Created or Cancelled,
            Created => requestedStatus is InProgress or Completed or Cancelled,
            InProgress => requestedStatus is Completed or Cancelled,
            _ => false
        };
    }
}
