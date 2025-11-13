using RippleSync.Application.Common.Notifiers;
using RippleSync.Application.Posts;

namespace RippleSync.API.PostPublisher;


/// <summary>
/// Background service used to fetch post ready to be published.
/// Posts that are ready to be published will be put into provided channel
/// </summary>
/// <param name="logger"></param>
/// <param name="intervalSeconds"></param>
/// <param name="postEventChannel"></param>
/// <param name="postManager"></param>
public class PostNotificationBackgroundService(
    ILogger<PostNotificationBackgroundService> logger,
    IServiceProvider serviceProvider,
    IPostNotificationListener notificationListener) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostScheduler started");

        // Process any existing posts on startup
        await ProcessReadyPosts(stoppingToken);

        // Start listening for notifications
        await notificationListener.StartListeningAsync(
            async () => await ProcessReadyPosts(stoppingToken),
            stoppingToken
        );
    }

    private async Task ProcessReadyPosts(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing ready posts");
            using var scope = serviceProvider.CreateScope();
            var postChannel = scope.ServiceProvider.GetRequiredService<PostChannel>();
            var postManager = scope.ServiceProvider.GetRequiredService<PostManager>();

            var posts = await postManager.GetPostReadyToPublish(cancellationToken);
            logger.LogInformation("Found {Count} posts ready to be published. Ids: {Ids}", posts.Count(), string.Join(", ", posts.Select(p => p.Id)));
            foreach (var post in posts)
            {
                await postChannel.PublishAsync(post);
            }
            logger.LogInformation("Published {Count} posts to channel", posts.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ready posts");
        }
    }
}
