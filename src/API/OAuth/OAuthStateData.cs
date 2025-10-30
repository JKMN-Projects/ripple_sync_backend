namespace RippleSync.API.Integrations;

public record OAuthStateData(
    Guid UserId,
    int PlatformId,
    string CodeVerifier
);
