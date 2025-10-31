namespace RippleSync.API.OAuth;

public record OAuthStateData(
    Guid UserId,
    int PlatformId,
    string CodeVerifier
);
