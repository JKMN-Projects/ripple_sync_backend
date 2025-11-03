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
public class PostSchedulingBackgroundService(ILogger<PostSchedulingBackgroundService> logger, int intervalSeconds, PostChannel postEventChannel, PostManager postManager) : BackgroundService
{
    private readonly PostManager _postManager = postManager;
    private readonly ILogger<PostSchedulingBackgroundService> _logger = logger;
    private readonly int _intervalSeconds = intervalSeconds;
    private readonly PostChannel _postChannel;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started with interval: {Interval}s", _intervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var postReadyToPublish = await _postManager.GetPostReadyToPublish(stoppingToken);
                foreach (var post in postReadyToPublish)
                {
                    await _postChannel.PublishAsync(post);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Background service");
                throw;
            }
            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }
    }
}
