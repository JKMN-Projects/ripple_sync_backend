using DbMigrator;

string? connectionString = args.FirstOrDefault();

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new ArgumentNullException(nameof(args));
}

bool verifyOnly = args.Length > 1 && args[1] == "--verify";

return DatabaseMigrator.MigrateDatabase(connectionString, verifyOnly);
