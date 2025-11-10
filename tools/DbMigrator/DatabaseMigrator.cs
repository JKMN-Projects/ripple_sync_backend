using DbUp;
using DbUp.Builder;
using System.Reflection;

namespace DbMigrator;
public class DatabaseMigrator
{
    public static int MigrateDatabase(string connectionString, bool verifyOnly = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        DbUp.Engine.DatabaseUpgradeResult? result;

        UpgradeEngineBuilder builder = DeployChanges.To
                         .PostgresqlDatabase(connectionString);


        builder = verifyOnly
            ? builder.WithTransactionAlwaysRollback()
            : builder.WithTransactionPerScript();

        builder = builder
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains(".migrations.")) // Always run migrations
            .LogToConsole();

        // Add test data scripts only in local environment
        if (IsLocalEnvironment())
        {
            builder = builder.WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains(".test_data."));
        }

        // If something like always recreating functions or views is wanted
        //builder = alwaysRun ?
        //     builder.JournalTo(new NullJournal()) :
        //     builder.JournalToSqlTable("dbo", "DatabaseMigrations");

        try
        {
            DbUp.Engine.UpgradeEngine upgrador = builder.Build();
            result = upgrador.PerformUpgrade();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e);
            Console.ResetColor();
            return -1;
        }

        if (result is not null && !result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        if (verifyOnly)
        {
            Console.WriteLine("Verification successful - all migrations can be applied!");
        }
        else
        {
            Console.WriteLine("All migrations applied!");
        }

        Console.ResetColor();

        return 0;
    }

    private static bool IsLocalEnvironment()
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env is "Development" or "Local";
    }
}
