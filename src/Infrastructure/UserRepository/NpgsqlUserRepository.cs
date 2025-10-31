using Npgsql;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.UserRepository.Entities;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class NpgsqlUserRepository(NpgsqlConnection dbConnection)
{
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        UserEntity? userEntity = null;

        try
        {
            userEntity = await dbConnection.SelectSingleOrDefaultAsync<UserEntity>(whereClause: "email = @email", param: new { email }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(this.GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userEntity == null ? null : User.Reconstitute(userEntity.Id, userEntity.Email, userEntity.PasswordHash, userEntity.Salt);
    }
}
