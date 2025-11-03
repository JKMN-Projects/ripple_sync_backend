using Npgsql;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Integrations;
using RippleSync.Application.Platforms;
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

        return userIntegrationEntites.Any() ? userIntegrationEntites.Select(i => new ConnectedIntegrationsResponse(i.Id, i.Name)) : [];
    }

    public async Task CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        var userPlatformIntegration = UserPlatformIntegrationEntity.New(integration.UserId, (int)integration.Platform, integration.AccessToken);

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

    public Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
