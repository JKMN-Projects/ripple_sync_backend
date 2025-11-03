using RippleSync.Application.Platforms;

namespace RippleSync.Application.Common.Queries;

public interface IPlatformQueries
{
    //Task<IEnumerable<PlatformResponse>> GetAllPlatformsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
