using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;

namespace RippleSync.Application.Platforms;
public class PlatformManager(ILogger<PostManager> logger,
    IPlatformQueries platformQueries)
{
    public async Task<ListResponse<PlatformResponse>> GetPlatformsAsync(CancellationToken cancellationToken = default)
    {
        var platforms = await platformQueries.GetAllPlatformsAsync(cancellationToken);

        return new ListResponse<PlatformResponse>(platforms);
    }
}
