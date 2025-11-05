namespace RippleSync.Application.Users;
public record AuthenticationResponse(
    string RefreshToken,
    string Email,
    long ExpiresAt);
