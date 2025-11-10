using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Responses;

namespace RippleSync.Application.Platforms;
public class PlatformManager(IPlatformQueries platformQueries)
{
    public async Task<ListResponse<PlatformResponse>> GetPlatformsAsync(CancellationToken cancellationToken = default)
    {
        var platforms = await platformQueries.GetAllPlatformsAsync(cancellationToken);

        return new ListResponse<PlatformResponse>(platforms);
    }
}
