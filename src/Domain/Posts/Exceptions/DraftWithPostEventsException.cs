namespace RippleSync.Domain.Posts.Exceptions;

public class DraftWithPostEventsException : Exception
{
    public DraftWithPostEventsException(string message) : base(message) { }

    public DraftWithPostEventsException()
        : this("Cannot create events for a draft post.") { }

}
