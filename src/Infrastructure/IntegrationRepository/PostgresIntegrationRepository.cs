using Npgsql;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.MicroORM.Exceptions;
using RippleSync.Infrastructure.MicroORM.Extensions;
using RippleSync.Infrastructure.UserRepository;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.UserPlatformIntegrationRepository;
internal class PostgresIntegrationRepository(NpgsqlConnection dbConnection)
{
    public async Task<IEnumerable<IntegrationResponse>> GetIntegrations(Guid userId, CancellationToken cancellationToken = default)
    {
        string getIntegrationsQuery =
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
            ExceptionFactory.ThrowRepositoryException(this.GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return integrationEntities.Any() ? integrationEntities.Select(i => new IntegrationResponse(i.Id, i.Name, i.Description, i.Connected, i.ImageUrl)) : [];
    }

    public async Task<IEnumerable<UserIntegrationResponse>> GetUserIntegrations(Guid userId, CancellationToken cancellationToken = default)
    {
        string getIntegrationsQuery =
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
            ExceptionFactory.ThrowRepositoryException(this.GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userIntegrationEntites.Any() ? userIntegrationEntites.Select(i => new UserIntegrationResponse(i.Id, i.Name)) : [];
    }

    public async Task CreateUserIntegration(Guid userId, int platformId, string accessToken, CancellationToken cancellationToken = default)
    {
        string getIntegrationsQuery =
            @"SELECT 
                p.id,
                p.platform_name AS name
            FROM platform p
            INNER JOIN user_platform_integration upi 
                ON p.id = upi.platform_id 
                AND upi.user_account_id = @userId
            ORDER BY p.platform_name;";

        try
        {
            int rowsAffected = await dbConnection.ExecuteAsync(getIntegrationsQuery, param: new { userId }, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected");

        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(this.GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task DeleteUserIntegration(Guid userId, int platformId, CancellationToken cancellationToken = default)
    {
        var toEdit = _integrations.FirstOrDefault(i => i.PlatformId == platformId);

        if (toEdit == null) return;

        int toEditIndex = _integrations.IndexOf(toEdit);
        _integrations[toEditIndex] = toEdit with { Connected = false };
    }
}
