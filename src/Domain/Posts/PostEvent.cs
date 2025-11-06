
namespace RippleSync.Domain.Posts;

public class PostEvent
{
    public Guid UserPlatformIntegrationId { get; set; }
    public PostStatus Status { get; set; }
    public string? PlatformPostIdentifier { get; set; }
    public object? PlatformResponse { get; set; }

    private PostEvent(Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        UserPlatformIntegrationId = userPlatformIntegrationId;
        Status = status;
        PlatformPostIdentifier = platformPostIdentifier;
        PlatformResponse = platformResponse;
    }

    public static PostEvent Create(Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        return new PostEvent(
            userPlatformIntegrationId: userPlatformIntegrationId,
            status: status,
            platformPostIdentifier: platformPostIdentifier,
            platformResponse: platformResponse
        );
    }

    public static PostEvent Reconstitute(Guid userPlatformIntegrationId, PostStatus status, string platformPostIdentifier, object? platformResponse)
    {
        return new PostEvent(
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
