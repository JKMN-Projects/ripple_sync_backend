using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;

namespace RippleSync.Application.Integrations;
public sealed class IntegrationManager(
        ILogger<IntegrationManager> logger,
        IUnitOfWork unitOfWork,
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
        TokenResponse tokenResponse,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            throw new ArgumentNullException(nameof(tokenResponse));

        if (!Enum.IsDefined(typeof(Platform), platformId))
        {
            throw new ArgumentOutOfRangeException(nameof(platformId), "Invalid platform ID");
        }

        DateTime expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        Integration integration = Integration.Create(userId, (Platform)platformId, tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt, tokenResponse.TokenType, tokenResponse.Scope);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await integrationRepo.CreateAsync(integration, cancellationToken));

        logger.LogInformation("User {UserId} created integration to {Platform}", userId, (Platform)platformId);
    }

    public async Task DeleteIntegrationAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await integrationRepo.DeleteAsync(userId, platform, cancellationToken));

        logger.LogInformation("User {UserId} removed integration to {Platform}", userId, platform);
    }
}
