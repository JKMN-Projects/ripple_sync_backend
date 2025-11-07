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
public class PostSchedulingBackgroundService(ILogger<PostSchedulingBackgroundService> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<PostSchedulingBackgroundService> _logger = logger;
    private readonly int _intervalSeconds = 10;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started with interval: {Interval}s", _intervalSeconds);
        var postChannel = serviceProvider.GetRequiredService<PostChannel>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var postManager = scope.ServiceProvider.GetRequiredService<PostManager>();
                var postReadyToPublish = await postManager.GetPostReadyToPublish(stoppingToken);
                foreach (var post in postReadyToPublish)
                {
                    await postChannel.PublishAsync(post);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Background service");
            }
            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }
    }
}
