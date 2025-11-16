using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.PostRepository.DapperEntities;
internal class GetPostsByUserResponseEntity
{
    public GetPostsByUserResponseEntity() { } // For Dapper

    public GetPostsByUserResponseEntity(Guid id, string messageContent, Guid[] mediaIds, string statusName, long? timestamp, string[] platforms)
    {
        Id = id;
        MessageContent = messageContent;
        MediaIds = mediaIds;
        StatusName = statusName;
        Timestamp = timestamp;
        Platforms = platforms;
    }

    public Guid Id { get; set; }
    public string MessageContent { get; set; }
    public Guid[] MediaIds { get; set; }
    public string StatusName { get; set; }
    public long? Timestamp { get; set; }
    public string[] Platforms { get; set; }
}