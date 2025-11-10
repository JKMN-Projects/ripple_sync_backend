using RippleSync.Domain.Posts;
using System.Threading.Channels;

namespace RippleSync.API.PostPublisher;

public class PostChannel
{
    private readonly Channel<Post> _channel;

    public PostChannel()
    {
        _channel = Channel.CreateBounded<Post>(new BoundedChannelOptions(1000)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait // Backpressure: scheduler blocks if full
        });
    }
    public async Task PublishAsync(Post message) => await _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<Post> ReadAllAsync() => _channel.Reader.ReadAllAsync();

    public bool TryRead(out Post? message) => _channel.Reader.TryRead(out message);

    public ChannelReader<Post> Reader => _channel.Reader;
}

