using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Tests.Common.Factories.Integrations;

public class IntegrationBuilder
{
    private readonly Guid _userId;
    private readonly Platform _platform;
    private string _accessToken = Guid.NewGuid().ToString();
    private string? _refreshToken = null;
    private DateTime? _expiresAt = null;
    private string? _tokenType = null;
    private string? _scope = null;

    public IntegrationBuilder(Guid userId, Platform platform)
    {
        _userId = userId;
        _platform = platform;
    }

    public IntegrationBuilder WithAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        return this;
    }
    public IntegrationBuilder WithRefreshToken(string refreshToken)
    {
        _refreshToken = refreshToken;
        return this;
    }
    public IntegrationBuilder ExpiresAt(DateTime expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }
    public IntegrationBuilder WithTokenType(string tokenType)
    {
        _tokenType = tokenType;
        return this;
    }
    public IntegrationBuilder WithScope(string scope)
    {
        _scope = scope;
        return this;
    }
    public Integration Build()
    {
        return Integration.Create(
            userId: _userId,
            platform: _platform,
            accessToken: _accessToken,
            refreshToken: _refreshToken,
            expiresAt: _expiresAt,
            tokenType: _tokenType,
            scope: _scope
        );
    }
}
