
namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageContent { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public IEnumerable<PostEvent> PostEvents { get; set; }
    public IEnumerable<PostMedia>? PostMedias { get; set; }

    private Post(Guid id, Guid userId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents, IEnumerable<PostMedia>? postMedias)
    {
        Id = id;
        UserId = userId;
        MessageContent = messageContent;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostEvents = postsEvents;
        PostMedias = postMedias;
    }

    public static Post Create(Guid userId, string messageContent, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents, IEnumerable<PostMedia>? postMedias)
    {
        return new Post(
            id: Guid.NewGuid(),
            userId: userId,
            messageContent: messageContent,
            updatedAt: DateTime.UtcNow,
            scheduledFor: scheduledFor,
            postsEvents: postsEvents,
            postMedias: postMedias
        );
    }

    public static Post Reconstitute(Guid id, Guid userId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents, IEnumerable<PostMedia>? postMedias)
    {
        return new Post(
            id: id,
            userId: userId,
            messageContent: messageContent,
            updatedAt: updatedAt,
            scheduledFor: scheduledFor,
            postsEvents: postsEvents,
            postMedias: postMedias
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
        var readyToPublish = ScheduledFor < DateTime.UtcNow && postStatus == PostStatus.Scheduled;
        return readyToPublish;
    }
    public PostStatus? GetPostMaxStatus()
    {
        var postStatus = PostEvents.MaxBy(pe => pe.Status)?.Status;
        return postStatus;
    }

    public Post Anonymize()
    {
        MessageContent = string.Empty;

        foreach (var postEvent in PostEvents)
        {
            postEvent.Anonymize();
        }
        foreach (var media in PostMedias ?? [])
        {
            media.Anonymize();
        }
        return this;


    }
}
