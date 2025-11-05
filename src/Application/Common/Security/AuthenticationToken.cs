using System.Security.Claims;

namespace RippleSync.Application.Common.Security;

public sealed record AuthenticationToken(
    string AccessToken,
    string TokenType,
    long ExpiresInMilliSeconds,
    IEnumerable<Claim> Claims);