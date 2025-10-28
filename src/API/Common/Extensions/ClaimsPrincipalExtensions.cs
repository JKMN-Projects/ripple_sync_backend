using RippleSync.Domain.Users;
using System.Security.Claims;

namespace RippleSync.API.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId)
            ? userId
            : throw new InvalidOperationException("Jwt failed to parse");

}
