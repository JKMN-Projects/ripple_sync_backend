using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;

namespace RippleSync.Application.Integrations;
public sealed class IntegrationManager(
        ILogger<IntegrationManager> logger,
        IIntegrationRepository integrationRepo,
        IIntegrationQueries integrationQueries,
        IPlatformQueries platformQueries)
{
    public async Task<ListResponse<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId)
        => new ListResponse<PlatformWithUserIntegrationResponse>(await platformQueries.GetPlatformsWithUserIntegrationsAsync(userId));

    public async Task<ListResponse<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId)
        => new ListResponse<ConnectedIntegrationsResponse>(await integrationQueries.GetConnectedIntegrationsAsync(userId));

    public async Task CreateIntegrationWithEncryptionAsync(
        Guid userId,
        int platformId,
        string accessToken,
        string? refreshToken,
        int expiresIn,
        string tokenType,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

        /// ENCRYPT ACCESSTOKEN HERE
        DateTime expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        Integration integration = Integration.Create(userId, platformId, accessToken, refreshToken, expiresAt, tokenType, scope);
        await integrationRepo.CreateAsync(integration, cancellationToken);
    }

    public async Task DeleteIntegrationAsync(Guid userId, int platformId, CancellationToken cancellationToken = default)
        => await integrationRepo.DeleteAsync(userId, platformId, cancellationToken);
}
