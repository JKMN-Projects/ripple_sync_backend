using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Integrations;
public sealed class IntegrationManager(
        ILogger<IntegrationManager> logger,
        IIntegrationRepository integrationRepo,
        IIntegrationQueries integrationQueries,
        IPlatformQueries platformQueries,
        IEncryptionService encryptor)
{
    public async Task<ListResponse<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId)
        => new ListResponse<PlatformWithUserIntegrationResponse>(await platformQueries.GetPlatformsWithUserIntegrationsAsync(userId));

    public async Task<ListResponse<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId)
        => new ListResponse<ConnectedIntegrationsResponse>(await integrationQueries.GetConnectedIntegrationsAsync(userId));

    public async Task CreateIntegrationWithEncryptionAsync(
        Guid userId,
        int platformId,
        TokenResponse tokenResponse,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken)) 
            throw new ArgumentNullException(nameof(tokenResponse));

        if (!Enum.IsDefined(typeof(Platform), platformId))
        {
            throw new ArgumentOutOfRangeException(nameof(platformId), "Invalid platform ID");
        }

        string encryptedAccessToken = encryptor.Encrypt(tokenResponse.AccessToken);

        string? encryptedRefreshToken = !string.IsNullOrWhiteSpace(tokenResponse.RefreshToken)
            ? encryptor.Encrypt(tokenResponse.RefreshToken)
            : null;

        /// ENCRYPT ACCESSTOKEN HERE
        DateTime expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        Integration integration = Integration.Create(userId, (Platform)platformId, encryptedAccessToken, encryptedRefreshToken, expiresAt, tokenResponse.TokenType, tokenResponse.Scope);
        await integrationRepo.CreateAsync(integration, cancellationToken);
    }

    public async Task DeleteIntegrationAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
        => await integrationRepo.DeleteAsync(userId, platform, cancellationToken);

}
