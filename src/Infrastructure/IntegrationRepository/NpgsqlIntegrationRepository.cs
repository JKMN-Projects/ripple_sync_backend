using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.IntegrationRepository.Entities;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;

namespace RippleSync.Infrastructure.IntegrationRepository;
internal class NpgsqlIntegrationRepository(
    IUnitOfWork uow,
    IEncryptionService encryptor) : BaseRepository(uow), IIntegrationRepository, IIntegrationQueries
{
    public async Task<IEnumerable<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string getConnectedIntegrationsQuery =
            @"SELECT 
                upi.id,
                p.platform_name
            FROM platform p
            INNER JOIN user_platform_integration upi 
                ON p.id = upi.platform_id 
                AND upi.user_account_id = @UserId
            ORDER BY p.platform_name;";

        IEnumerable<ConnectedIntegrationEntity> connectedIntegrationEntities = [];

        try
        {
            connectedIntegrationEntities = await Connection.QueryAsync<ConnectedIntegrationEntity>(getConnectedIntegrationsQuery, param: new { UserId = userId }, trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return connectedIntegrationEntities.Any() ? connectedIntegrationEntities.Select(i => new ConnectedIntegrationsResponse(i.Id, i.Name)) : [];
    }

    public async Task<IEnumerable<Integration>> GetIntegrationsByIdsAsync(IEnumerable<Guid> integrationIds, CancellationToken cancellationToken = default)
    {
        IEnumerable<UserPlatformIntegrationEntity> integrations = [];

        try
        {
            integrations = await Connection.SelectAsync<UserPlatformIntegrationEntity>("id = ANY(@IntegrationIds)", param: new { IntegrationIds = integrationIds }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrations.Select(i =>
            Integration.Reconstitute(i.Id, i.UserAccountId, (Platform)i.PlatformId, DecryptAccessToken(i.AccessToken), DecryptRefreshToken(i.RefreshToken), i.Expiration, i.TokenType, i.Scope)
        );
    }


    public async Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<UserPlatformIntegrationEntity> integrations = [];

        try
        {
            integrations = await Connection.SelectAsync<UserPlatformIntegrationEntity>("user_account_id = @UserId", param: new { UserId = userId }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrations.Select(i =>
            Integration.Reconstitute(i.Id, i.UserAccountId, (Platform)i.PlatformId, DecryptAccessToken(i.AccessToken), DecryptRefreshToken(i.RefreshToken), i.Expiration, i.TokenType, i.Scope)
        );
    }
    public async Task CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = new UserPlatformIntegrationEntity(integration.Id, integration.UserId, (int)integration.Platform, EncryptAccessToken(integration.AccessToken), EncryptRefreshToken(integration.RefreshToken), integration.ExpiresAt, integration.TokenType, integration.Scope);

        try
        {
            int rowsAffected = await Connection.InsertAsync(userPlatformIntegration, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on integration insert");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = new UserPlatformIntegrationEntity(integration.Id, integration.UserId, (int)integration.Platform, EncryptAccessToken(integration.AccessToken), EncryptRefreshToken(integration.RefreshToken), integration.ExpiresAt, integration.TokenType, integration.Scope);

        try
        {
            int rowsAffected = await Connection.UpdateAsync(userPlatformIntegration, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on integration update");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task DeleteAsync(Guid userId, Platform platform, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(userId, (int)platform);

        try
        {
            int rowsAffected = await Connection.RemoveAsync(userPlatformIntegration, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on integration remove");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    private string EncryptAccessToken(string accessToken)
        => !string.IsNullOrWhiteSpace(accessToken)
            ? encryptor.Encrypt(EncryptionTask.IntegrationAccessToken, accessToken)
            : string.Empty;

    private string DecryptAccessToken(string accessToken)
        => !string.IsNullOrWhiteSpace(accessToken)
            ? encryptor.Decrypt(EncryptionTask.IntegrationAccessToken, accessToken)
            : string.Empty;

    private string? EncryptRefreshToken(string? refreshToken)
        => !string.IsNullOrWhiteSpace(refreshToken)
            ? encryptor.Encrypt(EncryptionTask.IntegrationRefreshToken, refreshToken)
            : null;

    private string? DecryptRefreshToken(string? refreshToken)
       => !string.IsNullOrWhiteSpace(refreshToken)
            ? encryptor.Decrypt(EncryptionTask.IntegrationRefreshToken, refreshToken)
            : null;
}
