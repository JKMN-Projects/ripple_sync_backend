using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.MicroORM.ClassAttributes;
using RippleSync.Infrastructure.MicroORM.Exceptions;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Metadata;

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
    /// Executes a query and returns the result as an enumerable of type T
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
        List<T> data = new List<T>();

        var type = typeof(T);
        var constructors = type.GetConstructors();
        foreach (var constructorT in constructors)
        {
            var defined = Attribute.IsDefined(constructorT, typeof(SqlConstructor));
            var attribute = constructorT.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(SqlConstructor));

        }

        var sqlConstructors = constructors.Where(c => Attribute.IsDefined(c, typeof(SqlConstructor)));

        var constructor = typeof(T).GetConstructors().FirstOrDefault(c => Attribute.IsDefined(c, typeof(SqlConstructor)))
                          ?? throw new Exception($"No {nameof(SqlConstructor)} was defined in {typeof(T).FullName}");

        List<object?[]> dbValues = [];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                List<object?> rowValues = new List<object?>();
                foreach (var parameter in constructor.GetParameters())
                {
                    if (parameter.Name == null) continue;

                    string name = char.ToLower(c: parameter.Name[0], System.Globalization.CultureInfo.InvariantCulture)
                                      .ToString();

                    if (parameter.Name.Length > 1)
                        name += parameter.Name[1..];

                    try
                    {
                        rowValues.Add(reader[name] == DBNull.Value ? GetDefaultValue(parameter.ParameterType) : reader[name]);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        switch (nameBehavior)
                        {
                            case ParameterNameBehavior.FailOnNotFound:
                                throw;
                            case ParameterNameBehavior.NullOnNotFound:
                                rowValues.Add(null);
                                break;
                            case ParameterNameBehavior.DefaultOnNotFound:
                                rowValues.Add(GetDefaultValue(parameter.ParameterType));
                                break;
                            default:
                                break;
                        }
                    }
                }
                dbValues.Add(rowValues.ToArray());
            }
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        foreach (var value in dbValues)
        {
            data.Add((T)constructor.Invoke(value));
        }

        return data;
    }


    internal static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound)
    {
        T? data = default;

        var constructor = typeof(T).GetConstructors().First(c => Attribute.IsDefined(c, typeof(SqlConstructor)));

        if (constructor == null)
            throw new Exception($"No {typeof(SqlConstructor).Name} was defined in {typeof(T).FullName}");

        ParameterInfo[] parameters = constructor.GetParameters().Where(p => p.Name != null).ToArray();
        object?[] dbValues = new object[parameters.Length];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null || parameters[i].Name == null) continue;

                    string parameterName = parameters[i].Name!;

                    string name = char.ToLower(parameterName[0], System.Globalization.CultureInfo.InvariantCulture)
                                      .ToString();

                    if (parameterName.Length > 1)
                        name += parameterName[1..];

                    try
                    {
                        dbValues[i] = reader[name] == DBNull.Value ? GetDefaultValue(parameters[i].ParameterType) : reader[name];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        switch (nameBehavior)
                        {
                            case ParameterNameBehavior.FailOnNotFound:
                                throw;
                            case ParameterNameBehavior.NullOnNotFound:
                                dbValues[i] = null;
                                break;
                            case ParameterNameBehavior.DefaultOnNotFound:
                                dbValues[i] = GetDefaultValue(parameters[i].ParameterType);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        data = (T)constructor.Invoke(dbValues);

        return data;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, string whereClause)
    {
        return await SelectAsync<T>(conn, null, whereClause, "", "");
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="whereClause"></param>
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

    internal static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "")
    {
        T? data = default;

        string query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        data = await QuerySingleOrDefaultAsync<T>(conn, query, param, trans);

        return data;
    }

    private static string SelectQuery<T>(string whereClause = "", string overwriteSchemaName = "", string overwriteTableName = "")
    {
        var constructor = typeof(T).GetConstructors().First(c => Attribute.IsDefined(c, typeof(SqlConstructor)));

        if (constructor == null)
            throw new Exception($"No {nameof(SqlConstructor)} was defined in {typeof(T).FullName}");

        var sqlConstructor = constructor.GetCustomAttributes(false).OfType<SqlConstructor>().FirstOrDefault();

        if (sqlConstructor == null)
            throw new Exception($"No {nameof(SqlConstructor)} could be parsed from {typeof(T).FullName}");

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


    internal static void InsertParameters(this NpgsqlCommand cmd, object? param)
    {
        if (param != null)
        {
            var properties = param.GetType().GetProperties();
            int geometryParamIndex = 0;

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
    {
        return value ?? DBNull.Value;
    }

    private static object? GetDefaultValue(Type? t)
    {
        if (t?.IsValueType ?? false)
            return Activator.CreateInstance(t);

        return null;
    }
}
