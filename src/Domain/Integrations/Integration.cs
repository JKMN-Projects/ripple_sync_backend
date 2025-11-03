
using RippleSync.Domain.Platforms;

namespace RippleSync.Domain.Integrations;
public class Integration
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Platform Platform { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public string TokenType { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;

    private Integration(Guid id, Guid userId, Platform platform, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope)
    {
        Id = id;
        UserId = userId;
        Platform = platform;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        TokenType = tokenType;
        Scope = scope;
    }

    public static Integration Create(Guid userId, Platform platform, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope)
    {
        return new Integration(
            id: Guid.Empty,
            userId: userId,
            platform: platform,
            accessToken: accessToken,
            refreshToken: refreshToken,
            expiresAt: expiresAt,
            tokenType: tokenType,
            scope: scope
        );
    }

    public static Integration Reconstitute(Guid id, Guid userId, Platform platform, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope)
    {
        return new Integration(
            id: id,
            userId: userId,
            platform: platform,
            accessToken: accessToken,
            refreshToken: refreshToken,
            expiresAt: expiresAt,
            tokenType: tokenType,
            scope: scope
        );
    }
    public Integration Anonymize()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        TokenType = string.Empty;
        Scope = string.Empty;
        return this;
    }
}
