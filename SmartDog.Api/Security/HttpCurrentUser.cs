using System.Security.Claims;
using SmartDog.Application.Services;

namespace SmartDog.Api.Security;

/// <summary>
/// The user id from the validated JWT: its <c>sub</c> claim, which the JWT handler maps to
/// <see cref="ClaimTypes.NameIdentifier"/>. Service clients authenticated by API key have none.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
