using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostEventEntity
{
    [SqlProperty(update: UpdateAction.Where, propName: "post_id", isScopeIdentifier: true)]
    public Guid PostId { get; set; }

    [SqlProperty(update: UpdateAction.Where, propName: "user_platform_integration_id", isRecordIdentifier: true)]
    public Guid UserPlatformIntegrationId { get; set; }

    [SqlProperty(propName: "post_status_id")]
    public int PostStatusId { get; set; }

    [SqlProperty(propName: "platform_post_identifier")]
    public string PlatformPostIdentifier { get; set; }

    [SqlProperty(propName: "platform_response", dbType: NpgsqlDbType.Jsonb)]
    public string? PlatformResponse { get; set; }


    [SqlConstructor(tableName: "post_event")]
    public PostEventEntity(Guid post_id, Guid user_platform_integration_id, int post_status_id, string platform_post_identifier, string? platform_response)
    {
        PostId = post_id;
        UserPlatformIntegrationId = user_platform_integration_id;
        PostStatusId = post_status_id;
        PlatformPostIdentifier = platform_post_identifier;
        PlatformResponse = platform_response;
    }
}
