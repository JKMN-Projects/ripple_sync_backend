using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostEventEntity
{
    [SqlPropertyAttribute(update: UpdateAction.Where, propName: "post_id")]
    public Guid PostId { get; set; }

    [SqlPropertyAttribute(update: UpdateAction.Where, propName: "user_platform_integration_id")]
    public Guid UserPlatformIntegrationId { get; set; }

    [SqlPropertyAttribute(propName: "post_status_id")]
    public int PostStatusId { get; set; }

    [SqlPropertyAttribute(propName: "platform_post_identifier")]
    public string PlatformPostIdentifier { get; set; }

    [SqlPropertyAttribute(propName: "platform_response")]
    public string? PlatformResponse { get; set; }


    [SqlConstructorAttribute("ripple_sync")]
    internal PostEventEntity(Guid post_id, Guid user_platform_integration_id, int post_status_id, string platform_post_identifier, string? platform_response)
    {
        PostId = post_id;
        UserPlatformIntegrationId = user_platform_integration_id;
        PostStatusId = post_status_id;
        PlatformPostIdentifier = platform_post_identifier;
        PlatformResponse = platform_response;
    }
}
