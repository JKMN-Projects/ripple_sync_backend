using RippleSync.Application.Integrations;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default);
    Task CreateIntegration(Guid userId, int platformId, string accessToken, CancellationToken cancellationToken = default);
    Task DeleteIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default);
}