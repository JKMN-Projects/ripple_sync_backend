using RippleSync.Application.Integrations;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default);
    Task CreateUserIntegration(Guid userId, int platformId, string accessToken, CancellationToken cancellationToken = default);
    Task DeleteUserIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default);
}