
namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageContent { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public IEnumerable<PostEvent> PostEvents { get; private set; }
    public IEnumerable<PostMedia>? PostMedias { get; set; }

    public Post(Guid userId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents, IEnumerable<PostMedia>? postMedias)
    {
        Id = id;
        UserId = userId;
        MessageContent = messageContent;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostEvents = postsEvents;
        PostMedias = postMedias;
    }

    public static Post Create(Guid userId, string messageContent, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents)
    {
        return new Post(
            id: Guid.NewGuid(),
            userId: userId,
            messageContent: messageContent,
            updatedAt: DateTime.UtcNow,
            scheduledFor: scheduledFor,
            postsEvents: postsEvents
        );
    }

    public static Post Reconstitute(Guid id, Guid userId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents)
    {
        return new Post(
            id: id,
            userId: userId,
            messageContent: messageContent,
            updatedAt: updatedAt,
            scheduledFor: scheduledFor,
            postsEvents: postsEvents
        );
    }

public class PostEvent
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public PostStatus Status { get; set; }
    public string PlatformPostIdentifier { get; set; }
    public object PlatformResponse { get; set; }

}

public class PostMedia
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public required string ImageUrl { get; set; }
}
    public bool IsDeletable()
    {
        var latestStatus = PostEvents.MaxBy(pe => pe.Status)?.Status;
        return latestStatus is PostStatus.Draft or PostStatus.Scheduled;
    }

}
