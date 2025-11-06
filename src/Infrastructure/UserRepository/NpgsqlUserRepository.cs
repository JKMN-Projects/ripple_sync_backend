using RippleSync.Application.Common;
using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.UserRepository.Entities;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class NpgsqlUserRepository(IUnitOfWork uow) : BaseRepository(uow), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        User? user = null;

        try
        {
            var userEntity = await Connection.SelectSingleOrDefaultAsync<UserEntity>(whereClause: "email = @Email", param: new { Email = email }, ct: cancellationToken);

            if (userEntity != null)
            {
                user = await GetUserWithRefreshToken(userEntity, cancellationToken);
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return user;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = null;

        try
        {
            var userEntity = await Connection.SelectSingleOrDefaultAsync<UserEntity>(whereClause: "id = @Id", param: new { Id = userId }, ct: cancellationToken);

            if (userEntity != null)
            {
                user = await GetUserWithRefreshToken(userEntity, cancellationToken);
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return user;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        string getUserFromTokenValue = @"
            SELECT ua.*
            FROM user_account ua
            INNER JOIN user_token ut ON ua.id = ut.user_account_id
            WHERE ut.token_value = @TokenValue;";

        User? user = null;

        try
        {
            var userEntity = await Connection.QuerySingleOrDefaultAsync<UserEntity>(getUserFromTokenValue, param: new { TokenValue = refreshTokenValue }, trans: Transaction, ct: cancellationToken);

            if (userEntity != null)
            {
                user = await GetUserWithRefreshToken(userEntity, cancellationToken);
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return user;
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var userEntity = new UserEntity(user.Id, user.Email, user.PasswordHash, user.Salt, user.CreatedAt);

        try
        {
            int rowsAffected = await Connection.InsertAsync(userEntity, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on User insert");

            if (user.RefreshToken != null)
            {
                var userTokenEntity = new UserTokenEntity(user.RefreshToken.Id, user.Id, (int)user.RefreshToken.Type, user.RefreshToken.Value, user.RefreshToken.CreatedAt, user.RefreshToken.ExpiresAt);

                rowsAffected = await Connection.InsertAsync(userTokenEntity, trans: Transaction, ct: cancellationToken);

                if (rowsAffected <= 0)
                    throw new RepositoryException("No rows were affected on Token insert");
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var userEntity = new UserEntity(user.Id, user.Email, user.PasswordHash, user.Salt, user.CreatedAt);

        try
        {
            int rowsAffected = await Connection.UpdateAsync(userEntity, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on User update");

            if (user.RefreshToken != null)
            {
                var userTokenEntity = new UserTokenEntity(user.RefreshToken.Id, user.Id, (int)user.RefreshToken.Type, user.RefreshToken.Value, user.RefreshToken.CreatedAt, user.RefreshToken.ExpiresAt);

                rowsAffected = await Connection.SyncAsync(userTokenEntity, trans: Transaction, ct: cancellationToken);

                if (rowsAffected <= 0)
                    throw new RepositoryException("No rows were affected on Token insert");
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }


    private async Task<User> GetUserWithRefreshToken(UserEntity userEntity, CancellationToken cancellationToken = default)
    {
        var userTokenEntity = await Connection.SelectSingleOrDefaultAsync<UserTokenEntity>(whereClause: "user_account_id = @UserId AND token_type_id = @TokenType ORDER BY created_at ASC", param: new { UserId = userEntity.Id, TokenType = (int)UserTokenType.Refresh }, ct: cancellationToken);

        RefreshToken? refreshToken = null;

        if (userTokenEntity != null)
            refreshToken = RefreshToken.Reconstitute(userTokenEntity.Id, userTokenEntity.TokenValue, userTokenEntity.CreatedAt, userTokenEntity.ExpiresAt);

        User user = User.Reconstitute(userEntity.Id, userEntity.Email, userEntity.PasswordHash, userEntity.Salt, userEntity.CreatedAt, refreshToken);

        return user;
    }
}
