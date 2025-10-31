
namespace RippleSync.Domain.Posts;

public class PostEvent
{
    public Guid PostId { get; set; }
    public Guid UserPlatformIntegrationId { get; set; }
    public PostStatus Status { get; set; }
    public string PlatformPostIdentifier { get; set; }
    public object? PlatformResponse { get; set; }

}
