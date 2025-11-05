namespace RippleSync.Domain.Posts.Exceptions;

public class ScheduledWithNoPostEventsException : Exception
{
    public ScheduledWithNoPostEventsException(string message) : base(message) { }
    public ScheduledWithNoPostEventsException()
        : this("Cannot schedule a post without events.") { }
}
