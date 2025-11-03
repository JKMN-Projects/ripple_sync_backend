namespace RippleSync.Application.Posts;

public record GetPostsByUserResponse(
    Guid PostId,
    string MessageContent,
    string StatusName,
    Guid[] MediaIds,
    long? Timestamp,
    string[] Platforms);