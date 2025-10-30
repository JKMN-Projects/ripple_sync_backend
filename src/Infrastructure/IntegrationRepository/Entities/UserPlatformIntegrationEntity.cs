using Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.MicroORM.ClassAttributes;

namespace RippleSync.Infrastructure.IntegrationRepository.Entities;
internal class UserPlatformIntegrationEntity
{
    [SqlProperty(QueryAction.IgnoreInsert, UpdateAction.Where)]
    public Guid Id { get; set; }

    [SqlProperty(update: UpdateAction.Ignore)]
    public Guid UserAccountId { get; set; }

    [SqlProperty(update: UpdateAction.Ignore)]
    public int PlatformId { get; set; }

    [SqlProperty(propName: "access_token")]
    public string AccessToken { get; set; }


    [SqlConstructor("ripple_sync", "user_platform_integration")]
    internal UserPlatformIntegrationEntity(Guid id, Guid userAccountId, int platformId, string accessToken)
    {
        Id = id;
        UserAccountId = userAccountId;
        PlatformId = platformId;
        AccessToken = accessToken;
    }

    internal static UserPlatformIntegrationEntity New(Guid userAccountId, int platformId)
        => New(userAccountId, platformId, "");

    internal static UserPlatformIntegrationEntity New(Guid userAccountId, int platformId, string accessToken)
        => new(Guid.Empty, userAccountId, platformId, accessToken);
}
