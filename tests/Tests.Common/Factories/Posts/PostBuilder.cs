using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Tests.Common.Factories.Posts;

public class PostBuilder
{
    private readonly Guid _userId;
    private string _messageContent = "Default message content";
    private DateTime? _scheduledFor;
    private readonly List<string> _postMediaData = [];
    private readonly List<Integration> _integrationToPostTo = [];

    public PostBuilder(Guid userId)
    {
        _userId = userId;
    }

    public PostBuilder WithMessageContent(string messageContent)
    {
        _messageContent = messageContent;
        return this;
    }
    public PostBuilder ScheduledFor(DateTime scheduledFor)
    {
        _scheduledFor = scheduledFor;
        return this;
    }
    public PostBuilder AddRandomMedia()
    {
        const int sizeInBytes = 256 * 10;
        byte[] randomData = new byte[sizeInBytes];
        new Random().NextBytes(randomData);
        string base64Data = Convert.ToBase64String(randomData);
        _postMediaData.Add(base64Data);
        return this;
    }
    public PostBuilder PostedTo(Integration integration)
    {
        _integrationToPostTo.Add(integration);
        return this;
    }

    public Post Build()
    {
        PostStatus postStatus = _scheduledFor.HasValue
            ? PostStatus.Scheduled
            : PostStatus.Draft;
        var postEvents = _integrationToPostTo.Select(integration =>
            PostEvent.Create(
                userPlatformIntegrationId: integration.Id,
                status: postStatus,
                platformPostIdentifier: string.Empty,
                platformResponse: null
            )
        ).ToList();
        var postMedias = _postMediaData.Select(PostMedia.Create).ToList();
        return Post.Create(
            userId: _userId,
            messageContent: _messageContent,
            scheduledFor: _scheduledFor,
            postMedia: postMedias,
            postsEvents: postEvents
        );
    }
}
