using Npgsql;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.Tests;

public class DataSeeder
{
    private readonly NpgsqlConnection _dbConnection;

    public DataSeeder(NpgsqlConnection npgsqlConnection)
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
        List<string> valueSets = [];
        List<NpgsqlParameter> parameters = [];
        try
        {
            for (var i = 0; i < users.Count(); i++)
            {
                var user = users.ElementAt(i);
                valueSets.Add($"(@id{i}, @email{i}, @salt{i}, @password_hash{i}, @created_at{i})");
                parameters.AddRange(
                [
                    new NpgsqlParameter($"id{i}", user.Id),
                    new NpgsqlParameter($"email{i}", user.Email),
                    new NpgsqlParameter($"salt{i}", user.Salt),
                    new NpgsqlParameter($"password_hash{i}", user.PasswordHash),
                    new NpgsqlParameter($"created_at{i}", user.CreatedAt)
                ]);
            }

            var insertCommand = new NpgsqlCommand(
                @$"
INSERT INTO user_account (id, email, salt, password_hash, created_at) 
VALUES {string.Join(", ", valueSets)}",
                _dbConnection,
                transaction);
            insertCommand.Parameters.AddRange(parameters.ToArray());
            await insertCommand.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}