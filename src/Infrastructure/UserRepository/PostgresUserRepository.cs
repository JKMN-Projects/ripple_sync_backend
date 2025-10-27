using Npgsql;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.MicroORM.Exceptions;
using RippleSync.Infrastructure.MicroORM.Extensions;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class PostgresUserRepository
{
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string connString = "";

        UserEntity? userEntity = null;

        try
        {
            await using NpgsqlConnection npgConn = new(connString);

            userEntity = await npgConn.SelectSingleOrDefaultAsync<UserEntity>(whereClause: "email = @email", param: new { email });
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(this.GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userEntity == null ? null : User.Reconstitute(userEntity.Id, userEntity.Email, userEntity.PasswordHash, userEntity.Salt);
    }
}
