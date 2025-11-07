using Microsoft.Extensions.Logging;
using RippleSync.Application.Common;
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
        IUnitOfWork unitOfWork,
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

        DateTime expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        Integration integration = Integration.Create(userId, (Platform)platformId, encryptedAccessToken, encryptedRefreshToken, expiresAt, tokenResponse.TokenType, tokenResponse.Scope);
        unitOfWork.BeginTransaction();
        try
        {
            await integrationRepo.CreateAsync(integration, cancellationToken);
            unitOfWork.Save();
        }
        catch (Exception)
        {
            unitOfWork.Cancel();
            throw;
        }

        logger.LogInformation("User {UserId} created integration to {Platform}", userId, (Platform)platformId);
    }

    public async Task DeleteIntegrationAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        unitOfWork.BeginTransaction();
        try
        {
            await integrationRepo.DeleteAsync(userId, platform, cancellationToken);
            unitOfWork.Save();
        }
        catch (Exception)
        {
            unitOfWork.Cancel();
            throw;
        }


        logger.LogInformation("User {UserId} removed integration to {Platform}", userId, platform);
    }
}
