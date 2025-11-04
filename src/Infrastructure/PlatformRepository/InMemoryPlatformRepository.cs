
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;

namespace RippleSync.Infrastructure.PlatformRepository;
internal class InMemoryPlatformRepository : IPlatformQueries
{
    public Task<IEnumerable<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enum.GetValues<Platform>()
            .Select(p => new PlatformWithUserIntegrationResponse(
                    PlatformId: (int)p,
                    Name: p.ToString(),
                    Description: $"Description for {p}",
                    Connected: InMemoryData.Integrations.Any(i => i.UserId == userId && i.Platform == p),
                    ImageUrl: "")
            ));
}