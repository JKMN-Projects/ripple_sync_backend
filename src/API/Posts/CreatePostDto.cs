namespace RippleSync.API.Posts;

public record class CreatePostDto(string MessageContent, long? Timestamp, string[]? MediaAttachment, int[] IntegrationIds);
