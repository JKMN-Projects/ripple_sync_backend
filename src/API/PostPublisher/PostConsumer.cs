using RippleSync.Application.Posts;

namespace RippleSync.API.PostPublisher;

public class PostConsumer(PostChannel channel, ILogger<PostConsumer> logger, PostManager postManager) : BackgroundService
{
    private readonly PostChannel _channel = channel;
    private readonly ILogger<PostConsumer> _logger = logger;
    private readonly PostManager _postManager = postManager;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostEventConsumer started");

        await foreach (var message in _channel.ReadAllAsync().WithCancellation(stoppingToken))
        {
            try
            {
                await _postManager.ProcessPostEventAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing post: Post={PostId}", message.Id);
            }
        }

        _logger.LogInformation("PostEventConsumer stopped");
    }
}

