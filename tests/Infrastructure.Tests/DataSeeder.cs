using Npgsql;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.Tests;

public static class DataSeeder
{
    public sealed class Users
    {
        private readonly NpgsqlConnection _dbConnection;
        public Users(NpgsqlConnection npgsqlConnection)
        {
            _dbConnection = npgsqlConnection;
        }

        public async Task SeedUserAsync(User user)
            => await SeedUsersAsync(user);
        public async Task SeedUsersAsync(params User[] users)
            => await SeedUsersAsync(users as IEnumerable<User>);
        public async Task SeedUsersAsync(IEnumerable<User> users)
        {
            if (_dbConnection.State != System.Data.ConnectionState.Open)
                await _dbConnection.OpenAsync();
            var transaction = await _dbConnection.BeginTransactionAsync();
            List<string> insertUsersValueSets = [];
            List<NpgsqlParameter> insertUsersParameters = [];

            List<string> insertRefreshTokensValueSets = [];
            List<NpgsqlParameter> insertRefreshTokensParameters = [];
            try
            {
                for (var i = 0; i < users.Count(); i++)
                {
                    var user = users.ElementAt(i);
                    insertUsersValueSets.Add($"(@id{i}, @email{i}, @salt{i}, @password_hash{i}, @created_at{i})");
                    insertUsersParameters.AddRange(
                    [
                        new NpgsqlParameter($"id{i}", user.Id),
                        new NpgsqlParameter($"email{i}", user.Email),
                        new NpgsqlParameter($"salt{i}", user.Salt),
                        new NpgsqlParameter($"password_hash{i}", user.PasswordHash),
                        new NpgsqlParameter($"created_at{i}", user.CreatedAt)
                    ]);

                    if (user.RefreshToken is not null)
                    {
                        insertRefreshTokensValueSets.Add($"(@id{i}, @token_type_id{i}, @token_value{i}, @created_at{i}, @expires_at{i}, @user_account_id{i})");
                        insertRefreshTokensParameters.AddRange(
                        [
                            new NpgsqlParameter($"id{i}", user.RefreshToken.Id),
                            new NpgsqlParameter($"token_type_id{i}",   (int)user.RefreshToken.Type),
                            new NpgsqlParameter($"token_value{i}", user.RefreshToken.Value),
                            new NpgsqlParameter($"created_at{i}", user.RefreshToken.CreatedAt),
                            new NpgsqlParameter($"expires_at{i}", user.RefreshToken.ExpiresAt),
                            new NpgsqlParameter($"user_account_id{i}", user.Id)
                        ]);
                    }
                }

                var insertCommand = new NpgsqlCommand(
                    @$"
    INSERT INTO user_account (id, email, salt, password_hash, created_at) 
    VALUES {string.Join(", ", insertUsersValueSets)}",
                    _dbConnection,
                    transaction);
                insertCommand.Parameters.AddRange(insertUsersParameters.ToArray());
                await insertCommand.ExecuteNonQueryAsync();

                if (insertRefreshTokensValueSets.Count > 0)
                {
                    var insertTokensCommand = new NpgsqlCommand(
                        @$"
    INSERT INTO user_token (id, token_type_id, token_value, created_at, expires_at, user_account_id) 
    VALUES {string.Join(", ", insertRefreshTokensValueSets)}",
                        _dbConnection,
                        transaction);
                    insertTokensCommand.Parameters.AddRange(insertRefreshTokensParameters.ToArray());
                    await insertTokensCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public sealed class Platforms
    {
        private readonly NpgsqlConnection _dbConnection;
        public Platforms(NpgsqlConnection npgsqlConnection)
        {
            _dbConnection = npgsqlConnection;
        }

        public async Task SeedPlatformsAsync(IEnumerable<Platform> platforms)
        {
            if (_dbConnection.State != System.Data.ConnectionState.Open)
                await _dbConnection.OpenAsync();
            List<string> valueSets = [];
            List<NpgsqlParameter> parameters = [];

            for (var i = 0; i < platforms.Count(); i++)
            {
                var platform = platforms.ElementAt(i);
                valueSets.Add($"(@id{i}, @name{i}, @description{i})");
                parameters.AddRange(
                [
                    new NpgsqlParameter($"id{i}", (int)platform),
                    new NpgsqlParameter($"name{i}", platform.ToString()),
                    new NpgsqlParameter($"description{i}", "platform.Description")
                ]);
            }
            var insertCommand = new NpgsqlCommand(
                @$"
    INSERT INTO platform (id, platform_name, platform_description)
    VALUES {string.Join(", ", valueSets)}",
                _dbConnection);
            insertCommand.Parameters.AddRange(parameters.ToArray());
            await insertCommand.ExecuteNonQueryAsync();
        }
    }
}