using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class GetPostsByUserResponseEntity
{
    public Guid Id { get; set; }

    [SqlPropertyAttribute(propName: "message_content")]
    public string MessageContent { get; set; }


    [SqlPropertyAttribute(propName: "media_ids")]
    public Guid[] MediaIds { get; set; }


    [SqlPropertyAttribute(propName: "status_name")]
    public string StatusName { get; set; }
    public long? Timestamp { get; set; }
    public string[] Platforms { get; set; }


    [SqlConstructorAttribute("ripple_sync")]
    internal GetPostsByUserResponseEntity(Guid id, string message_content, Guid[] media_ids, string status_name, long? timestamp, string[] platforms)
    {
        Id = id;
        MessageContent = message_content;
        MediaIds = media_ids;
        StatusName = status_name;
        Timestamp = timestamp;
        Platforms = platforms;
    }
}
