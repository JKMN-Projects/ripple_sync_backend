using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class GetPostsByUserResponseEntity
{
    public Guid Id { get; set; }

    [SqlProperty(propName: "message_content")]
    public string MessageContent { get; set; }


    [SqlProperty(propName: "media_ids")]
    public Guid[] MediaIds { get; set; }


    [SqlProperty(propName: "status_name")]
    public string StatusName { get; set; }
    public long? Timestamp { get; set; }
    public string[] Platforms { get; set; }


    [SqlConstructor()]
    public GetPostsByUserResponseEntity(Guid id, string message_content, Guid[] media_ids, string status_name, long? timestamp, string[] platforms)
    {
        Id = id;
        MessageContent = message_content;
        MediaIds = media_ids;
        StatusName = status_name;
        Timestamp = timestamp;
        Platforms = platforms;
    }
}
