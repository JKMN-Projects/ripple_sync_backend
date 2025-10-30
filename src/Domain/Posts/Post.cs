
namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public IEnumerable<PostEvent> PostEvents { get; set; }
    public IEnumerable<PostMedia>? PostMedias { get; set; }

    public Post(Guid UserId, string messageContent, DateTime updatedAt, DateTime? scheduledFor, IEnumerable<PostEvent> postsEvents, IEnumerable<PostMedia>? postMedias)
    {
        Id = Guid.NewGuid();
        this.UserId = UserId;
        MessageContent = messageContent;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostEvents = postsEvents;
        PostMedias = postMedias;
    }

    public bool IsDeletable()
    {
        var latestStatus = PostEvents.MaxBy(pe => pe.Status)?.Status;
        return latestStatus is PostStatus.Draft or PostStatus.Scheduled;
    }
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

public enum PostStatus
{
    Draft = 0,
    Scheduled,
    Posted,
    Processing,
    Failed
}