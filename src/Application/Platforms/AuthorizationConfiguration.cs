namespace RippleSync.Application.Platforms;

public record AuthorizationConfiguration(
    string RedirectUri,
    string State,
    string CodeChallenge
);
