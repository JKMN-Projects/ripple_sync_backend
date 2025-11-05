using System.Security.Claims;

namespace RippleSync.API.Common.Extensions;

public static class ClaimEnumerableExtensions
{
    public static string? FindUserId(this IEnumerable<Claim> claims)
        => claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

    public static string? FindEmail(this IEnumerable<Claim> claims)
        => claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
}