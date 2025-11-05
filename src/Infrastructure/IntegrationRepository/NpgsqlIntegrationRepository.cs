using Npgsql;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Infrastructure.IntegrationRepository.Entities;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;

namespace RippleSync.Infrastructure.IntegrationRepository;
internal class NpgsqlIntegrationRepository(NpgsqlConnection dbConnection) : IIntegrationRepository, IIntegrationQueries
{
    public async Task<IEnumerable<ConnectedIntegrationsResponse>> GetConnectedIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var getConnectedIntegrationsQuery =
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
            connectedIntegrationEntities = await dbConnection.QueryAsync<ConnectedIntegrationEntity>(getConnectedIntegrationsQuery, param: new { UserId = userId }, ct: cancellationToken);
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
            integrations = await dbConnection.SelectAsync<UserPlatformIntegrationEntity>("id = ANY(@IntegrationIds)", param: new { IntegrationIds = integrationIds }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrations.Select(i => Integration.Reconstitute(i.Id, i.UserAccountId, (Platform)i.PlatformId, i.AccessToken, i.RefreshToken, i.Expiration, i.TokenType, i.Scope));
    }


    public async Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<UserPlatformIntegrationEntity> integrations = [];

        try
        {
            integrations = await dbConnection.SelectAsync<UserPlatformIntegrationEntity>("user_account_id = @UserId", param: new { UserId = userId }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrations.Select(i => Integration.Reconstitute(i.Id, i.UserAccountId, (Platform)i.PlatformId, i.AccessToken, i.RefreshToken, i.Expiration, i.TokenType, i.Scope));
    }
    public async Task CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(integration.UserId, (int)integration.Platform, integration.AccessToken, integration.RefreshToken, integration.ExpiresAt, integration.TokenType, integration.Scope);

        try
        {
            int rowsAffected = await dbConnection.InsertAsync(userPlatformIntegration, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(integration.UserId, (int)integration.Platform, integration.AccessToken, integration.RefreshToken, integration.ExpiresAt, integration.TokenType, integration.Scope);

        try
        {
            int rowsAffected = await dbConnection.UpdateAsync(userPlatformIntegration, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected");
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
            int rowsAffected = await dbConnection.RemoveAsync(userPlatformIntegration, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }
}
