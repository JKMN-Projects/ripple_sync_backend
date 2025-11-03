namespace RippleSync.Application.Platforms;

public record TokenAccessConfiguration(
    string RedirectUri,
    string Code,
    string CodeVerifier
);
