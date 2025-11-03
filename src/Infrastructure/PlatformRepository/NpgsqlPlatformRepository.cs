using Npgsql;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Platforms;
using RippleSync.Infrastructure.IntegrationRepository.Entities;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;

namespace RippleSync.Infrastructure.PlatformRepository;

internal class NpgsqlPlatformRepository(
    NpgsqlConnection dbConnection) : IPlatformQueries
{
    public async Task<IEnumerable<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
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

        return integrationEntities.Any() ? integrationEntities.Select(i => new PlatformWithUserIntegrationResponse(i.Id, i.Name, i.Description, i.Connected, i.ImageUrl)) : [];
    }
}
