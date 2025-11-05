using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.PostRepository.Entities;
internal class PostMediaEntity
{
    [SqlPropertyAttribute(action: QueryAction.IgnoreInsert, update: UpdateAction.Where)]
    public Guid Id { get; set; }

    [SqlPropertyAttribute(update: UpdateAction.Where, propName: "post_id")]
    public Guid PostId { get; set; }

    [SqlPropertyAttribute(propName: "image_data")]
    public string ImageData { get; set; }


    [SqlConstructorAttribute("ripple_sync")]
    internal PostMediaEntity(Guid id, Guid post_id, string image_data)
    {
        Id = id;
        PostId = post_id;
        ImageData = image_data;
    }
}
