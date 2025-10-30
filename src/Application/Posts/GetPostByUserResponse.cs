namespace RippleSync.Application.Posts;

public record GetPostsByUserResponse(Guid PostId, string MessageContent, string StatusName, string[] MediaAttachment, long? Timestamp, string[] Platforms);