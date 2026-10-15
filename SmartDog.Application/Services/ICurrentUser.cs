namespace SmartDog.Application.Services;

/// <summary>The signed-in user on whose behalf a request reads or changes decisions.</summary>
public interface ICurrentUser
{
    /// <summary>The user id from the access token's subject, or null when the request has none.</summary>
    Guid? UserId { get; }
}

public static class CurrentUserExtensions
{
    /// <summary>
    /// The owner that every decision query is scoped to. Throws instead of returning null, because a null owner would
    /// match decisions that have no owner.
    /// </summary>
    public static Guid RequireUserId(this ICurrentUser user) =>
        user.UserId ?? throw new UnauthorizedAccessException("The request has no authenticated user id.");
}

/// <summary>Cache keys for decisions include the owner, so a cached decision can only be served to its owner.</summary>
public static class DecisionCacheKeys
{
    public static string Decision(Guid ownerId, Guid decisionId) => $"decision:{ownerId}:{decisionId}";
}
