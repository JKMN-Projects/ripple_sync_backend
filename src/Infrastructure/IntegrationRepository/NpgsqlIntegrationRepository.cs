using Npgsql;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using RippleSync.Infrastructure.IntegrationRepository.Entities;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;

namespace RippleSync.Infrastructure.IntegrationRepository;
internal class NpgsqlIntegrationRepository(NpgsqlConnection dbConnection) : IIntegrationRepository
{
    public async Task<IEnumerable<IntegrationResponse>> GetIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var getIntegrationsQuery =
            @"SELECT 
                p.id,
                p.platform_name AS name,
                CASE WHEN upi.id IS NOT NULL THEN true ELSE false END AS connected,
                p.platform_description AS description,
                p.image_url AS ""imageUrl""
            FROM platform p
            LEFT JOIN user_platform_integration upi 
                ON p.id = upi.platform_id 
                AND upi.user_account_id = @userId
            ORDER BY p.platform_name;";

        IEnumerable<IntegrationEntity> integrationEntities = [];

        try
        {
            integrationEntities = await dbConnection.QueryAsync<IntegrationEntity>(getIntegrationsQuery, param: new { userId }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrationEntities.Any() ? integrationEntities.Select(i => new IntegrationResponse(i.Id, i.Name, i.Description, i.Connected, i.ImageUrl)) : [];
    }

    public async Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default)
    {
        var getIntegrationsQuery =
            @"SELECT 
                p.id,
                p.platform_name AS name
            FROM platform p
            INNER JOIN user_platform_integration upi 
                ON p.id = upi.platform_id 
                AND upi.user_account_id = @userId
            ORDER BY p.platform_name;";

        IEnumerable<UserIntegrationEntity> userIntegrationEntites = [];

        try
        {
            userIntegrationEntites = await dbConnection.QueryAsync<UserIntegrationEntity>(getIntegrationsQuery, param: new { userId }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userIntegrationEntites.Any() ? userIntegrationEntites.Select(i => new UserIntegrationResponse(i.Id, i.Name)) : [];
    }

    public async Task CreateIntegration(Guid userId, int platformId, string accessToken, string? refreshToken, DateTime expiresAt, string tokenType, string scope, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(userId, platformId, accessToken);

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

    public async Task DeleteIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(userId, platformId);

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
