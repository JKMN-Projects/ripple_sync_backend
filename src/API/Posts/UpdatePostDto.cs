namespace RippleSync.API.Posts;

public record class UpdatePostDto(int PostId, string MessageContent, long? Timestamp, string[]? MediaAttachment, int[] IntegrationIds);
