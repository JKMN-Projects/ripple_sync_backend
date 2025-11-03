using RippleSync.Application.Posts;

namespace RippleSync.API.PostPublisher;

public class PostConsumer(ILogger<PostConsumer> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<PostConsumer> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = serviceProvider.GetRequiredService<PostChannel>();
        _logger.LogInformation("PostEventConsumer started");
        await foreach (var message in channel.ReadAllAsync().WithCancellation(stoppingToken))
        {
            try
            {
                using var serviceScope = serviceProvider.CreateScope();
                var postManager = serviceScope.ServiceProvider.GetRequiredService<PostManager>();
                await postManager.ProcessPostEventAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing post: Post={PostId}", message.Id);
            }
        }

        _logger.LogInformation("PostEventConsumer stopped");
    }
}

