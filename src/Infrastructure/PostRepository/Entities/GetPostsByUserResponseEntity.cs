using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.PostRepository.Entities;

[method: SqlConstructor()]
internal class GetPostsByUserResponseEntity(Guid id, string messageContent, Guid[] mediaIds, string statusName, long? timestamp, string[] platforms)
{
    public Guid Id { get; set; } = id;
    public string MessageContent { get; set; } = messageContent;
    public Guid[] MediaIds { get; set; } = mediaIds;
    public string StatusName { get; set; } = statusName;
    public long? Timestamp { get; set; } = timestamp;
    public string[] Platforms { get; set; } = platforms;
}
