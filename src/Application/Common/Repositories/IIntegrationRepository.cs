using RippleSync.Application.Integrations;

namespace RippleSync.Application.Common.Repositories;
public interface IIntegrationRepository
{
    // Queries
    Task<IEnumerable<IntegrationResponse>> GetIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Domain Actions
    Task CreateIntegrationAsync(Guid userId, int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope, CancellationToken cancellationToken = default);
    Task DeleteIntegrationAsync(Guid userId, int platformId, CancellationToken cancellationToken = default);
}