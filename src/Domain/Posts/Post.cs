
using RippleSync.Domain.Posts.Exceptions;
using System.Collections.Generic;

namespace RippleSync.Domain.Posts;

public class Post
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageContent { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public List<PostMedia> PostMedia { get; set; }
    public List<PostEvent> PostEvents { get; set; }

    private Post(Guid id, Guid userId, string messageContent, DateTime submittedAt, DateTime? updatedAt, DateTime? scheduledFor, IEnumerable<PostMedia> postMedia, IEnumerable<PostEvent> postsEvents)
    {
        Id = id;
        UserId = userId;
        MessageContent = messageContent;
        MessageContent = messageContent;
        SubmittedAt = submittedAt;
        UpdatedAt = updatedAt;
        ScheduledFor = scheduledFor;
        PostMedia = postMedia.ToList();
        PostEvents = postsEvents.ToList();
    }

    public static Post Create(Guid userId, string messageContent, DateTime? scheduledFor, IEnumerable<PostMedia> postMedia, IEnumerable<PostEvent> postsEvents)
    {
        if (scheduledFor != null && !postsEvents.Any())
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
            postMedia: postMedia,
            postsEvents: postsEvents
        );
    }

    public static Post Reconstitute(Guid id, Guid userId, string messageContent, DateTime submittedAt, DateTime? updatedAt, DateTime? scheduledFor, IEnumerable<PostMedia> postMedia, IEnumerable<PostEvent> postsEvents)
    {
        return new Post(
            id: id,
            userId: userId,
            messageContent: messageContent,
            submittedAt: submittedAt,
            updatedAt: updatedAt,
            scheduledFor: scheduledFor,
            postMedia: postMedia,
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
        var readyToPublish = ScheduledFor < DateTime.UtcNow;
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
        foreach (var media in PostMedia)
        {
            media.Anonymize();
        }

        return this;
    }
}
