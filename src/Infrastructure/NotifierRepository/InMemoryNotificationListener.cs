using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Notifiers;

namespace RippleSync.Infrastructure.NotifierRepository;
internal class InMemoryNotificationListener(ILogger<InMemoryNotificationListener> logger) : IPostNotificationListener
{
    public async Task StartListeningAsync(Func<Task> onNotificationReceived, CancellationToken cancellationToken)
    {
        logger.LogInformation("In-memory polling listener started");

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            if (InMemoryData.Posts.Any(p => p.ScheduledFor <= DateTime.UtcNow))
            {
                logger.LogDebug("Ready posts detected");
                await onNotificationReceived();
            }
        }

        logger.LogInformation("In-memory polling listener stopped");
    }
}
