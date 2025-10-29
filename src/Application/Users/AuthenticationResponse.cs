namespace RippleSync.Application.Users;
public record AuthenticationResponse(
    string Email,
    long ExpiresAt);
