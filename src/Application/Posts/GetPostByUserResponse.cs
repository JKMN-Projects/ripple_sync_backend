namespace RippleSync.Application.Posts;

public record GetPostsByUserResponse(
    Guid PostId,
    string MessageContent,
    Guid[] MediaIds,
    string StatusName,
    long? TimestampUnix,
    string[] Platforms);