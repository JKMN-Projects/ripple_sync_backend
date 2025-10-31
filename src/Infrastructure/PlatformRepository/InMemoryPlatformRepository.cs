
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Platforms;

namespace RippleSync.Infrastructure.PlatformRepository;
internal class InMemoryPlatformRepository : IPlatformQueries
{
    public Task<IEnumerable<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult((IEnumerable<PlatformWithUserIntegrationResponse>)InMemoryData.IntegrationResponses);
}