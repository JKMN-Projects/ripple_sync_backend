
namespace RippleSync.Application.Users;
public record AuthenticationTokenResponse(
    string Token,
    string TokenType,
    long ExpiresAt,
    string RefreshToken,
    long RefreshTokenExpiresAt);
