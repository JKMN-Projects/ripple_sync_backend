using RippleSync.Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;
internal class UserPlatformIntegrationEntity
{
    [SqlProperty(update: UpdateAction.Ignore)]
    public Guid Id { get; set; }

    [SqlProperty(update: UpdateAction.Where, propName: "user_account_id")]
    public Guid UserAccountId { get; set; }

    [SqlProperty(update: UpdateAction.Where, propName: "platform_id")]
    public int PlatformId { get; set; }

    [SqlProperty(propName: "access_token")]
    public string AccessToken { get; set; }

    [SqlProperty(propName: "refresh_token")]
    public string? RefreshToken { get; set; }
    public DateTime? Expiration { get; set; }

    [SqlProperty(propName: "token_type")]
    public string? TokenType { get; set; }
    public string? Scope { get; set; }


    [SqlConstructor(tableName: "user_platform_integration")]
    public UserPlatformIntegrationEntity(Guid id, Guid user_account_id, int platform_id, string access_token, string? refresh_token, DateTime? expiration, string? token_type, string? scope)
    {
        Id = id;
        UserAccountId = user_account_id;
        PlatformId = platform_id;
        AccessToken = access_token;
        RefreshToken = refresh_token;
        Expiration = expiration;
        TokenType = token_type;
        Scope = scope;
    }

    internal static UserPlatformIntegrationEntity New(Guid userAccountId, int platformId)
        => new(Guid.Empty, userAccountId, platformId, "", null, null, null, null);
}
