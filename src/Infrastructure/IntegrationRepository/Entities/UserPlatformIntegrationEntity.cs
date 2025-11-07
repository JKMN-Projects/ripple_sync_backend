using RippleSync.Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;

[method: SqlConstructor(tableName: "user_platform_integration")]
internal class UserPlatformIntegrationEntity(Guid id, Guid userAccountId, int platformId, string accessToken, string? refreshToken, DateTime? expiration, string? tokenType, string? scope)
{
    [SqlProperty(update: UpdateAction.Ignore)]
    public Guid Id { get; set; } = id;

    [SqlProperty(update: UpdateAction.Where)]
    public Guid UserAccountId { get; set; } = userAccountId;

    [SqlProperty(update: UpdateAction.Where)]
    public int PlatformId { get; set; } = platformId;
    public string AccessToken { get; set; } = accessToken;
    public string? RefreshToken { get; set; } = refreshToken;
    public DateTime? Expiration { get; set; } = expiration;
    public string? TokenType { get; set; } = tokenType;
    public string? Scope { get; set; } = scope;

    internal static UserPlatformIntegrationEntity New(Guid userAccountId, int platformId)
        => new(Guid.Empty, userAccountId, platformId, "", null, null, null, null);
}
