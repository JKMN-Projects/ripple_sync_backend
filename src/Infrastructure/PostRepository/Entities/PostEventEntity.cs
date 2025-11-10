using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;

[method: SqlConstructor(tableName: "post_event")]
internal class PostEventEntity(Guid postId, Guid userPlatformIntegrationId, int postStatusId, string? platformPostIdentifier, string? platformResponse)
{
    [SqlProperty(update: UpdateAction.Where, isScopeIdentifier: true)]
    public Guid PostId { get; set; } = postId;

    [SqlProperty(update: UpdateAction.Where, isRecordIdentifier: true)]
    public Guid UserPlatformIntegrationId { get; set; } = userPlatformIntegrationId;
    public int PostStatusId { get; set; } = postStatusId;
    public string? PlatformPostIdentifier { get; set; } = platformPostIdentifier;

    [SqlProperty(dbType: NpgsqlDbType.Jsonb)]
    public string? PlatformResponse { get; set; } = platformResponse;
}
