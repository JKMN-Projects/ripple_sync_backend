using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostMediaEntity
{
    [SqlProperty(update: UpdateAction.Where, isRecordIdentifier: true)]
    public Guid Id { get; set; }

    [SqlProperty(update: UpdateAction.Where, propName: "post_id")]
    public Guid PostId { get; set; }

    [SqlProperty(propName: "image_data")]
    public string ImageData { get; set; }


    [SqlConstructor(tableName: "post_media")]
    public PostMediaEntity(Guid id, Guid post_id, string image_data)
    {
        Id = id;
        PostId = post_id;
        ImageData = image_data;
    }
}
