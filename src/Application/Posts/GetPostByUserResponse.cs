namespace RippleSync.Application.Posts;

public record GetPostsByUserResponse(
    Guid PostId,
    string MessageContent,
    string[] MediaAttachment,
    string StatusName,
    long? TimestampUnix,
    string[] Platforms);