using System.Text.Json.Serialization;

namespace RippleSync.Application.Platforms;

public record TokenResponse(
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken
);
