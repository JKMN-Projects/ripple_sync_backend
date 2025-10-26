
namespace RippleSync.Application.Users;
public record AuthenticationTokenResponse(
    string Token,
    string TokenType,
    int ExpiresIn);
