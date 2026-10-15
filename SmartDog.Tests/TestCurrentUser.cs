using SmartDog.Application.Services;

namespace SmartDog.Tests;

/// <summary>A fixed signed-in user for service tests.</summary>
internal sealed class TestCurrentUser(Guid? userId) : ICurrentUser
{
    public static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static TestCurrentUser Owner { get; } = new(OwnerId);
    public Guid? UserId { get; } = userId;
}
