using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using System;

namespace RippleSync.Infrastructure.IntegrationRepository;

public class InMemoryIntegrationRepository : IIntegrationRepository
{
    public record Integration(int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope);

    public Task<IEnumerable<IntegrationResponse>> GetIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<IntegrationResponse>>(InMemoryData.IntegrationResponses);

    public Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(InMemoryData.IntegrationResponses.Select(i => new UserIntegrationResponse(i.PlatformId, i.Name)));

    public Task CreateIntegrationAsync(Guid userId, int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope, CancellationToken cancellationToken = default)
    {
        UpdateIntegration(platformId, true);
        return Task.CompletedTask;
    }

    public Task DeleteIntegrationAsync(Guid userId, int platformId, CancellationToken cancellationToken = default)
    {
        UpdateIntegration(platformId, false);
        return Task.CompletedTask;
    }

    private static void UpdateIntegration(int platformId, bool connected)
    {
        var toEdit = InMemoryData.IntegrationResponses.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        var toEditIndex = InMemoryData.IntegrationResponses.IndexOf(toEdit);
        InMemoryData.IntegrationResponses[toEditIndex] = toEdit with { Connected = connected };
    }
}
