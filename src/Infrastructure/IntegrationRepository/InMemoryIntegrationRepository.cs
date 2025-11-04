using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Infrastructure.IntegrationRepository;

public class InMemoryIntegrationRepository : IIntegrationRepository, IIntegrationQueries
{
    public Task<IEnumerable<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(InMemoryData.Integrations.Select(i => new ConnectedIntegrationsResponse(i.Id, i.Platform.ToString())));

    public Task CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        InMemoryData.Integrations.Add(integration);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        InMemoryData.Integrations.RemoveAll(i => i.UserId == userId && i.Platform == platform);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(InMemoryData.Integrations.Where(i => i.UserId == userId));
    }
    public Task UpdateAsync(Integration integration, CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }
}
