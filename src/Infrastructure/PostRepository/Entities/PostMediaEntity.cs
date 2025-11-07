using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;

[method: SqlConstructor(tableName: "post_media")]
internal class PostMediaEntity(Guid id, Guid postId, string imageData)
{
    [SqlProperty(update: UpdateAction.Where, isRecordIdentifier: true)]
    public Guid Id { get; set; } = id;

    [SqlProperty(update: UpdateAction.Where)]
    public Guid PostId { get; set; } = postId;
    public string ImageData { get; set; } = imageData;
}
