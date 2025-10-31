﻿using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Infrastructure.IntegrationRepository;

public class InMemoryIntegrationRepository : IIntegrationRepository, IIntegrationQueries
{
    public Task<IEnumerable<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(InMemoryData.IntegrationResponses.Select(i => new ConnectedIntegrationsResponse(i.PlatformId, i.Name)));

    public Task CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        UpdateIntegration(integration.Platform, true);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        UpdateIntegration(platform, false);
        return Task.CompletedTask;
    }

    private static void UpdateIntegration(Platform platform, bool connected)
    {
        var toEdit = InMemoryData.IntegrationResponses.FirstOrDefault(i => i.PlatformId == (int)platform);

        if (toEdit == null) return;

        var toEditIndex = InMemoryData.IntegrationResponses.IndexOf(toEdit);
        InMemoryData.IntegrationResponses[toEditIndex] = toEdit with { Connected = connected };
    }
}
