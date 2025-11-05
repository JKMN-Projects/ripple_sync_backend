
using RippleSync.Domain.Posts.Exceptions;

namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageContent { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public IEnumerable<PostMedia> PostMedias { get; set; }
    public IEnumerable<PostEvent> PostEvents { get; set; }

    private Post(Guid id, Guid userId, string messageContent, DateTime submittedAt, DateTime? updatedAt, DateTime? scheduledFor, IEnumerable<PostMedia> postMedias, IEnumerable<PostEvent> postsEvents)
    {
        Id = id;
        UserId = userId;
        MessageContent = messageContent;
        MessageContent = messageContent;
        SubmittedAt = submittedAt;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostMedias = postMedias;
        PostEvents = postsEvents;
    }

    public static Post Create(Guid userId, string messageContent, DateTime? scheduledFor, IEnumerable<PostMedia> postMedias, IEnumerable<PostEvent> postsEvents)
    {
        if (scheduledFor != null && postsEvents.Any() is false)
            throw new ScheduledWithNoPostEventsException();
        else if (scheduledFor is null && postsEvents.Any())
            throw new DraftWithPostEventsException();

        return new Post(
            id: Guid.NewGuid(),
            userId: userId,
            messageContent: messageContent,
            submittedAt: DateTime.UtcNow,
            updatedAt: null,
            scheduledFor: scheduledFor,
            postMedias: postMedias,
            postsEvents: postsEvents
        );
    }

    public static Post Reconstitute(Guid id, Guid userId, string messageContent, DateTime submittedAt, DateTime? updatedAt, DateTime? scheduledFor, IEnumerable<PostMedia> postMedias, IEnumerable<PostEvent> postsEvents)
    {
        return new Post(
            id: id,
            userId: userId,
            messageContent: messageContent,
            submittedAt: submittedAt,
            updatedAt: updatedAt,
            scheduledFor: scheduledFor,
            postMedias: postMedias,
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
