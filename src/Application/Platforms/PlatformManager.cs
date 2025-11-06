using Microsoft.Extensions.Logging;
using RippleSync.Application.Common;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
