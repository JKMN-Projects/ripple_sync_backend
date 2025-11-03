
namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageContent { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public IEnumerable<PostEvent> PostEvents { get; private set; }

    private Post(Guid id, Guid userId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents)
    {
        Id = id;
        UserId = userId;
        MessageContent = messageContent;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostEvents = postsEvents;
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

    public bool IsDeletable()
    {
        var postStatus = GetPostMaxStatus();
        return postStatus is PostStatus.Draft or PostStatus.Scheduled;
    }
    public bool IsReadyToPublish()
    {
        var postStatus = GetPostMaxStatus();
        var readyToPublish = ScheduledFor < DateTime.Now && postStatus == PostStatus.Scheduled;
        return readyToPublish;
    }
    public PostStatus? GetPostMaxStatus()
    {
        var postStatus = PostEvents.MaxBy(pe => pe.Status)?.Status;
        return postStatus;
    }

}
