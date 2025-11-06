using RippleSync.Application.Common;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Platforms;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.PlatformRepository.Entities;

namespace RippleSync.Infrastructure.PlatformRepository;

internal class NpgsqlPlatformRepository(IUnitOfWork uow) : BaseRepository(uow), IPlatformQueries
{
    public Task<IEnumerable<PlatformResponse>> GetAllPlatformsAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<PlatformResponseEntity> platformEntities = [];

        try
        {
            platformEntities = await Connection.QueryAsync<PlatformResponseEntity>("SELECT p.platform_name FROM platform p", trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return platformEntities.Any() ? platformEntities.Select(i => new PlatformResponse(i.Name)) : [];
    }

    public async Task<IEnumerable<PlatformWithUserIntegrationResponse>> GetPlatformsWithUserIntegrationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var getPlatformsQuery =
            @"SELECT 
                p.id,
                p.platform_name,
                CASE WHEN upi.id IS NOT NULL THEN true ELSE false END AS connected,
                p.platform_description,
                p.image_data
            FROM platform p
            LEFT JOIN user_platform_integration upi 
                ON p.id = upi.platform_id 
                AND upi.user_account_id = @UserId
            ORDER BY p.platform_name;";

        IEnumerable<PlatformIntegrationResponseEntity> platformEntities = [];

        try
        {
            platformEntities = await Connection.QueryAsync<PlatformIntegrationResponseEntity>(getPlatformsQuery, param: new { UserId = userId }, trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return platformEntities.Any() ? platformEntities.Select(i => new PlatformWithUserIntegrationResponse(i.Id, i.Name, i.Description, i.Connected, i.ImageData)) : [];
    }
}
