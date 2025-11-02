using RippleSync.Domain.Posts;
using System.Threading.Channels;

namespace RippleSync.API;

public class PostChannel
{
    private readonly Channel<Post> _channel;

    public PostChannel()
    {
        _channel = Channel.CreateUnbounded<Post>(new UnboundedChannelOptions()
        {
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = false
        });
    }
    public async Task PublishAsync(Post message) => await _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<Post> ReadAllAsync() => _channel.Reader.ReadAllAsync();

    public bool TryRead(out Post message) => _channel.Reader.TryRead(out message);

    public ChannelReader<Post> Reader => _channel.Reader;
}

