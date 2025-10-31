using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using System;

namespace RippleSync.Infrastructure.IntegrationRepository;
public class InMemoryIntegrationRepository : IIntegrationRepository
{
    public record Integration(int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope);
    public async Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId, CancellationToken cancellationToken = default)
        => _integrations;

    public async Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default)
        => _integrations.Select(i => new UserIntegrationResponse(i.PlatformId, i.Name));

    public async Task CreateIntegration(Guid userId, int platformId, string accessToken, CancellationToken cancellationToken = default)
        => await UpdateIntegration(platformId, true);

    public async Task DeleteIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default)
        => await UpdateIntegration(platformId, false);

    private async Task UpdateIntegration(int platformId, bool connected)
    {
        var toEdit = InMemoryData.IntegrationResponses.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        var toEditIndex = _integrations.IndexOf(toEdit);
        _integrations[toEditIndex] = toEdit with { Connected = connected };
    }
}
