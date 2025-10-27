using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.MicroORM.ClassAttributes;
using RippleSync.Infrastructure.MicroORM.Exceptions;
using System.Reflection;

namespace RippleSync.Infrastructure.MicroORM.Extensions;
internal static class NpgsqlExtensions
{

    internal enum ParameterNameBehavior
    {
        FailOnNotFound,
        NullOnNotFound,
        DefaultOnNotFound
    }

    private static BindingFlags _acquirePropFlags = BindingFlags.FlattenHierarchy | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Executes the query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T">The type to map the results to</typeparam>
    /// <param name="conn">The database connection</param>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="param">The parameters object (anonymous object or DynamicParameters)</param>
    /// <param name="nameBehavior">Parameter name behavior</param>
    /// <returns>An enumerable of type T</returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> QueryAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound)
    {
        var constructor = GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters().Where(p => p.Name != null)];

        List<object?[]> dbValues = [];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dbValues.Add(ReadRow(reader, parameters, nameBehavior));
            }
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        return dbValues.Select(values => (T)constructor.Invoke(values));
    }

    /// <summary>
    /// Executes the query and returns a single result of type T or Default of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="query"></param>
    /// <param name="param"></param>
    /// <param name="trans"></param>
    /// <param name="nameBehavior"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="QueryException"></exception>
    internal static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound)
    {
        var constructor = GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters().Where(p => p.Name != null)];

        object?[] dbValues = new object[parameters.Length];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dbValues = ReadRow(reader, parameters, nameBehavior);
            }
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        return (T)constructor.Invoke(dbValues);
    }

    /// <summary>
    /// Executes a select query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, string whereClause)
        => await SelectAsync<T>(conn, null, whereClause, "", "");

    /// <summary>
    /// Executes a select query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "")
    {
        IEnumerable<T> data;

        string query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        data = await conn.QueryAsync<T>(query, param, trans);

        return data;
    }

    /// <summary>
    /// Executes a select query and returns a single result of type T or Default of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="trans"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="param"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <returns></returns>
    internal static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "")
    {
        string query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        var data = await conn.QuerySingleOrDefaultAsync<T>(query, param, trans);
        return data;
    }

    /// <summary>
    /// Create Select Query to run in cmd
    /// </summary>
    /// <typeparam name="T"></typeparam>s
    /// <param name="whereClause"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string SelectQuery<T>(string whereClause = "", string overwriteSchemaName = "", string overwriteTableName = "")
    {
        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        string where = whereClause;

        if (!string.IsNullOrWhiteSpace(where))
        {
            where = where.Trim();
            if (!where.StartsWith("WHERE", StringComparison.InvariantCultureIgnoreCase))
                where = "WHERE " + where;
        }

        return $"SELECT * FROM {schemaName}{tableName} {where}";
    }

    /// <summary>
    /// Reads a single row from the data reader and maps it to constructor parameters
    /// </summary>
    private static object?[] ReadRow(NpgsqlDataReader reader, ParameterInfo[] parameters, ParameterNameBehavior nameBehavior)
    {
        object?[] values = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            string? parameterName = parameters[i].Name;
            if (string.IsNullOrWhiteSpace(parameterName)) continue;

            string name = char.ToLower(parameterName[0], System.Globalization.CultureInfo.InvariantCulture).ToString();

            if (parameterName.Length > 1)
                name += parameterName[1..];

            try
            {
                values[i] = reader[name] == DBNull.Value ? GetDefaultValue(parameters[i].ParameterType) : reader[name];
            }
            catch (IndexOutOfRangeException)
            {
                switch (nameBehavior)
                {
                    case ParameterNameBehavior.FailOnNotFound:
                        throw;
                    case ParameterNameBehavior.NullOnNotFound:
                        values[i] = null;
                        break;
                    case ParameterNameBehavior.DefaultOnNotFound:
                        values[i] = GetDefaultValue(parameters[i].ParameterType);
                        break;
                    default:
                        break;
                }
            }
        }

        return values;
    }

    /// <summary>
    /// Gets the constructor marked with SqlConstructor attribute
    /// </summary>
    private static ConstructorInfo GetSqlConstructor<T>()
    {
        return typeof(T).GetConstructors().FirstOrDefault(c => Attribute.IsDefined(c, typeof(SqlConstructor)))
           ?? throw new InvalidOperationException($"No {nameof(SqlConstructor)} was defined in {typeof(T).FullName}");
    }

    /// <summary>
    /// Gets the constructor marked with SqlConstructor attribute as an SqlConstructor
    /// </summary>
    private static SqlConstructor GetConstructorOfTypeSqlConstructor<T>()
    {
        var constructor = GetSqlConstructor<T>();

        return constructor.GetCustomAttributes(false).OfType<SqlConstructor>().FirstOrDefault()
            ?? throw new InvalidOperationException($"No {nameof(SqlConstructor)} could be parsed from {typeof(T).FullName}");
    }

    /// <summary>
    /// Extrapolates Schema and Table name
    ///     Prioritizes overwrites, followed by <see cref="SqlConstructor"/>, then by class name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="overwriteTableName"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="ctorSqlAttribute"></param>
    /// <returns>schema and table name as tuple</returns>
    private static (string schemaName, string tableName) GetSchemaAndTableName<T>(string overwriteTableName, string overwriteSchemaName, SqlConstructor? ctorSqlAttribute = null)
    {
        string table = overwriteTableName;

        if (string.IsNullOrWhiteSpace(table))
            table = ctorSqlAttribute?.TableName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(table))
        {
            string name = typeof(T).Name;

            if (name.Length > 0)
                table = name[0].ToString().ToLowerInvariant();

            if (name.Length > 1)
                table += name[1..];
        }

        if (!string.IsNullOrWhiteSpace(table) && !table.Trim().EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
            table = "\"" + table.Trim() + "\"";

        string schema = overwriteSchemaName;

        if (string.IsNullOrWhiteSpace(schema))
            schema = ctorSqlAttribute?.SchemaName ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(schema) && !schema.Trim().EndsWith(".", StringComparison.InvariantCultureIgnoreCase))
            schema = "\"" + schema.Trim() + "\".";

        return (schema, table);
    }

    /// <summary>
    /// Adds <see cref="NpgsqlParameter"/>s to the cmd, from the parameter objects
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    internal static void InsertParameters(this NpgsqlCommand cmd, object? param)
    {
        if (param != null)
        {
            var properties = param.GetType().GetProperties();

            foreach (var property in properties)
            {
                Type type = property.PropertyType;
                Type? underlyingType = Nullable.GetUnderlyingType(type);
                object? dataValue = property.GetValue(param);

                object paramValue = dataValue ?? GetDefaultValue(underlyingType ?? type) ?? DBNull.Value;
                var paramName = property.Name.StartsWith("@", StringComparison.InvariantCultureIgnoreCase) ? property.Name : $"@{property.Name}";

                var parameter = new NpgsqlParameter(paramName, paramValue)
                {
                    NpgsqlDbType = GetNpgsqlDbType(underlyingType ?? type)
                };

                cmd.Parameters.Add(parameter);
            }
        }
    }

    /// <summary>
    /// Maps C# types to their corresponding NpgsqlDbType
    /// </summary>
    private static NpgsqlDbType GetNpgsqlDbType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType switch
        {
            var t when t == typeof(int) => NpgsqlDbType.Integer,
            var t when t == typeof(long) => NpgsqlDbType.Bigint,
            var t when t == typeof(short) => NpgsqlDbType.Smallint,
            var t when t == typeof(string) => NpgsqlDbType.Text,
            var t when t == typeof(bool) => NpgsqlDbType.Boolean,
            var t when t == typeof(decimal) => NpgsqlDbType.Numeric,
            var t when t == typeof(double) => NpgsqlDbType.Double,
            var t when t == typeof(float) => NpgsqlDbType.Real,
            var t when t == typeof(DateTime) => NpgsqlDbType.Timestamp,
            var t when t == typeof(DateTimeOffset) => NpgsqlDbType.TimestampTz,
            var t when t == typeof(TimeSpan) => NpgsqlDbType.Interval,
            var t when t == typeof(Guid) => NpgsqlDbType.Uuid,
            var t when t == typeof(byte[]) => NpgsqlDbType.Bytea,
            _ => NpgsqlDbType.Unknown // Let Npgsql infer as fallback
        };
    }

    public static object ToDbValue(this object? value)
        => value ?? DBNull.Value;

    private static object? GetDefaultValue(Type? t)
        => t?.IsValueType ?? false ? Activator.CreateInstance(t) : null;
}
