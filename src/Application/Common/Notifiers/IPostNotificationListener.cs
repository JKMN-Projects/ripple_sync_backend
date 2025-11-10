namespace RippleSync.Application.Common.Notifiers;
public interface IPostNotificationListener
{
    Task StartListeningAsync(Func<Task> onNotificationReceived, CancellationToken cancellationToken);
}
