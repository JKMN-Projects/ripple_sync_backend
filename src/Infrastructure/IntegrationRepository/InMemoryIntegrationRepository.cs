using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;

namespace RippleSync.Infrastructure.UserPlatformIntegrationRepository;
public class InMemoryIntegrationRepository : IIntegrationRepository
{
    public record Integration(int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope);

    private static readonly List<Integration> _integrations = [];

    public async Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId)
        => InMemoryData.IntegrationResponses;

    public async Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId)
        => InMemoryData.IntegrationResponses.Select(i => new UserIntegrationResponse(i.PlatformId, i.Name));

    public async Task CreateUserIntegration(Guid userId, int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope)
    {
        var toEdit = InMemoryData.IntegrationResponses.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        int toEditIndex = InMemoryData.IntegrationResponses.IndexOf(toEdit);
        InMemoryData.IntegrationResponses[toEditIndex] = toEdit with { Connected = true };

        _integrations.Add(new Integration(platformId, accessToken, refreshToken, expiresAt, tokenType, scope));
    }

    public async Task DeleteUserIntegration(Guid userId, int platformId)
    {
        var toEdit = InMemoryData.IntegrationResponses.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        int toEditIndex = InMemoryData.IntegrationResponses.IndexOf(toEdit);
        InMemoryData.IntegrationResponses[toEditIndex] = toEdit with { Connected = false };
    }
}
