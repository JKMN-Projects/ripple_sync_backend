using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using System;

namespace RippleSync.Infrastructure.IntegrationRepository;
public class InMemoryIntegrationRepository : IIntegrationRepository
{
    private static readonly List<IntegrationResponse> _integrations =
    [
        new (1, "Twitter", "Share updates on Twitter", false, ""),
        new (2, "Facebook", "Create posts on Facebook", false, ""),
        new (3, "LinkedIn", "Share professional updates on LinkedIn", true, ""),
        new (4, "Instagram", "Post photos and stories on Instagram", false, "")
    ];

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
        var toEdit = _integrations.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        var toEditIndex = _integrations.IndexOf(toEdit);
        _integrations[toEditIndex] = toEdit with { Connected = connected };
    }
}
