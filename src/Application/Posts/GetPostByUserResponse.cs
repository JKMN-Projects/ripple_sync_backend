namespace RippleSync.Application.Posts;

public record GetPostsByUserResponse(int id, string MessageContent, string StatusName, string[] MediaAttachment, long Timestamp, string[] Platforms);