
namespace RippleSync.Domain.Posts;

public class PostEvent
{
    public Guid PostId { get; set; }
    public Guid UserPlatformIntegrationId { get; set; }
    public PostStatus Status { get; set; }
    public string PlatformPostIdentifier { get; set; }
    public object? PlatformResponse { get; set; }

    private PostEvent(Guid postId, Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        PostId = postId;
        UserPlatformIntegrationId = userPlatformIntegrationId;
        Status = status;
        PlatformPostIdentifier = platformPostIdentifier;
        PlatformResponse = platformResponse;
    }

    public static PostEvent New(Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        return new PostEvent(
            postId: Guid.NewGuid(),
            userPlatformIntegrationId: userPlatformIntegrationId,
            status: status,
            platformPostIdentifier: platformPostIdentifier,
            platformResponse: platformResponse
        );
    }

    public static PostEvent Reconstitute(Guid postId, Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        return new PostEvent(
            postId: postId,
            userPlatformIntegrationId: userPlatformIntegrationId,
            status: status,
            platformPostIdentifier: platformPostIdentifier,
            platformResponse: platformResponse
        );
    }

    public PostEvent Anonymize()
    {
        PlatformPostIdentifier = string.Empty;
        PlatformResponse = null;
        return this;
    }
}
