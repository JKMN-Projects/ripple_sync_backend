using RippleSync.Application.Platforms;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.API;

public class PostConsumer(PostChannel channel, ILogger<PostConsumer> logger, PostManager postManager, IPlatformFactory platformFactory) : BackgroundService
{
    private readonly PostChannel _channel = channel;
    private readonly ILogger<PostConsumer> _logger = logger;
    private readonly PostManager _postManager = postManager;
    private readonly IPlatformFactory _platformFactory = platformFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostEventConsumer started");

        await foreach (var message in _channel.ReadAllAsync().WithCancellation(stoppingToken))
        {
            try
            {
                foreach (var postEvent in message.PostEvents)
                {
                    await ProcessPostEventAsync(postEvent, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing post event: Post={PostId} on Platform={PlatformId}", message.PostId, message.PlatformPostIdentifier);
            }
        }

        _logger.LogInformation("PostEventConsumer stopped");
    }
    private async Task ProcessPostEventAsync(PostEvent postEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing post event: PostId={PostId}, Platform={Platform}",
            postEvent.PostId,
            postEvent.PlatformPostIdentifier);
        try
        {
            postEvent.Status = PostStatus.Processing;
            postEvent = await _postManager.UpdatePostEventAsync(postEvent);

            //Request platform
            IPlatform platform = _platformFactory.Create(Domain.Platforms.Platform.X);


            //If its Post, we should foreach post Event. but if its a post event we need to look up post information first and then publish that one post
            platform.PublishPostAsync();


            postEvent.Status = PostStatus.Posted;
            await _postManager.UpdatePostEventAsync(postEvent);
            _logger.LogInformation(
                "Postevent has been published: PostId={PostId}, Platform={Platform}",
                postEvent.PostId,
                postEvent.PlatformPostIdentifier);



        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to publish post for : PostId={PostId}, Platform={Platform} Exception {Exception}",
                postEvent.PostId,
                postEvent.PlatformPostIdentifier,
                ex.Message
            );
            postEvent.Status = PostStatus.Failed;
            await _postManager.UpdatePostEventAsync(postEvent);
            throw;
        }

    }
}
