using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using RippleSync.Application.Common.Notifiers;

namespace RippleSync.Infrastructure.NotifierRepository;
internal class NpgsqlNotificationListener(
    ILogger<NpgsqlNotificationListener> logger,
    IConfiguration configuration) : IPostNotificationListener
{
    private readonly string _connectionString = configuration.GetConnectionString("Postgres") + ";Application Name=NotificationListener"
            ?? throw new InvalidOperationException("Connection string not found");

    public async Task StartListeningAsync(Func<Task> onNotificationReceived, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        connection.Notification += async (sender, args) =>
        {
            logger.LogDebug("Received 'posts_ready' notification");
            await onNotificationReceived();
        };

        const string listenSql = "LISTEN posts_ready";
        await using (var cmd = new NpgsqlCommand(listenSql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Listening on channel 'posts_ready'");

        while (!cancellationToken.IsCancellationRequested)
        {
            await connection.WaitAsync(cancellationToken);
        }

        logger.LogInformation("Stopped listening on 'posts_ready'");
    }
}
