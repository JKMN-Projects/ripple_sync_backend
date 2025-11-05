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


    [SqlPropertyAttribute(propName: "media_attachment")]
    public string[] MediaAttachment { get; set; }


    [SqlPropertyAttribute(propName: "status_name")]
    public string StatusName { get; set; }
    public long? Timestamp { get; set; }
    public string[] Platforms { get; set; }


    [SqlConstructorAttribute("ripple_sync")]
    internal GetPostsByUserResponseEntity(Guid id, string message_content, string[] media_attachment, string status_name, long? timestamp, string[] platforms)
    {
        Id = id;
        MessageContent = message_content;
        MediaAttachment = media_attachment;
        StatusName = status_name;
        Timestamp = timestamp;
        Platforms = platforms;
    }
}
