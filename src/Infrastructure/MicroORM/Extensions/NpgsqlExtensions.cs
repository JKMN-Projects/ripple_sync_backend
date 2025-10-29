using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.MicroORM.ClassAttributes;
using RippleSync.Infrastructure.MicroORM.Exceptions;
using System.Reflection;

namespace RippleSync.Infrastructure.MicroORM.Extensions;
internal static class NpgsqlExtensions
{
    /// <summary>
    /// How the ORM should behave when names aren't found
    /// </summary>
    internal enum ParameterNameBehavior
    {
        FailOnNotFound,
        NullOnNotFound,
        DefaultOnNotFound
    }

    /// <summary>
    /// Flags for acquiring all instance properties (public and private) from a type and its base classes.
    /// </summary>
    private static readonly BindingFlags _acquirePropFlags = BindingFlags.FlattenHierarchy | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Executes the query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T">The type to map the results to</typeparam>
    /// <param name="conn">The database connection</param>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="param">The parameters object (anonymous object or DynamicParameters)</param>
    /// <param name="trans"></param>
    /// <param name="nameBehavior">Parameter name behavior</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>An enumerable of type T</returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> QueryAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
    {
        var constructor = GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters().Where(p => p.Name != null)];

        List<object?[]> dbValues = [];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
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
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="QueryException"></exception>
    internal static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
    {
        var constructor = GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters().Where(p => p.Name != null)];

        object?[] dbValues = new object[parameters.Length];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
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
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, string whereClause, CancellationToken ct = default)
        => await SelectAsync<T>(conn, null, whereClause, "", "", ct: ct);

    /// <summary>
    /// Executes a select query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="trans"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="param"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        IEnumerable<T> data;

        string query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        data = await conn.QueryAsync<T>(query, param, trans, ct: ct);

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
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    internal static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        string query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        var data = await conn.QuerySingleOrDefaultAsync<T>(query, param, trans, ct: ct);
        return data;
    }

    /// <summary>
    /// Executes a non-query command and returns the number of affected rows
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="param">The parameters object (anonymous object)</param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    internal static async Task<int> ExecuteAsync(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, CancellationToken ct = default)
    {
        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.InsertParameters(param);

        return await LoggedExecuteNonQueryAsync(cmd, ct); ;
    }

    /// <summary>
    /// Inserts a record into the database table corresponding to Type T.
    /// Insertion is based on properties not marked with <see cref="QueryAction.IgnoreInsert"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    internal static async Task<int> InsertAsync<T>(this NpgsqlConnection conn, T data, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
        => await conn.InsertAsync(new[] { data }, trans, overwriteSchemaName, overwriteTableName, ct);

    /// <summary>
    /// Inserts multiple records into the database table corresponding to Type T.
    /// Insertion is based on properties not marked with <see cref="QueryAction.IgnoreInsert"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    internal static async Task<int> InsertAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        if (datas == null || !datas.Any())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        string insert = $"INSERT INTO {schemaName}{tableName}(";
        string sqlValStart = ") VALUES ";

        List<string> colFields = [];

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlProperty>().FirstOrDefault();
            if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
            {
                continue;
            }

            string name = GetPropertyName(attribute, property);

            colFields.Add($"\"{name}\"");
        }

        var parameters = new List<NpgsqlParameter>();
        var rowValuePlaceholders = new List<string>();
        int paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowPlaceholders = new List<string>();

            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlProperty>().FirstOrDefault();
                if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
                {
                    continue;
                }

                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);
                parameters.AddRange(propParams);
                rowPlaceholders.Add(placeholder);
            }

            rowValuePlaceholders.Add($"({string.Join(", ", rowPlaceholders)})");
        }

        string query = insert + string.Join(", ", colFields) + sqlValStart + string.Join(", ", rowValuePlaceholders);

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        int rowsAffected = await LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Removes a record from the database table corresponding to Type T.
    /// Deletion is based on properties marked with <see cref="UpdateAction.Where"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="UpdateAction.Where"/> were defined on Type T</exception>
    internal static async Task<int> RemoveAsync<T>(this NpgsqlConnection conn, T data, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
        => await conn.RemoveAsync(new[] { data }, trans, overwriteSchemaName, overwriteTableName, ct);

    /// <summary>
    /// Removes multiple records from the database table corresponding to Type T.
    /// Deletion is based on properties marked with <see cref="UpdateAction.Where"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="UpdateAction.Where"/> were defined on Type T</exception>
    internal static async Task<int> RemoveAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        if (datas == null || !datas.Any())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var parameters = new List<NpgsqlParameter>();
        var whereConditions = new List<string>();
        int paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowConditions = [];
            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlProperty>().FirstOrDefault();

                if (attribute == null || attribute.Update != UpdateAction.Where)
                {
                    continue;
                }

                string name = GetPropertyName(attribute, property);
                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);

                parameters.AddRange(propParams);
                rowConditions.Add($"\"{name}\" = {placeholder}");
            }

            if (rowConditions.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no properties marked with UpdateAction.Where for deletion.");

            whereConditions.Add($"({string.Join(" AND ", rowConditions)})");
        }

        string query = $"DELETE FROM {schemaName}{tableName} WHERE {string.Join(" OR ", whereConditions)}";

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        int rowsAffected = await LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Create Select Query to run in cmd
    /// </summary>
    /// <typeparam name="T"></typeparam>s
    /// <param name="whereClause"></param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructor"/></param>
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
    /// <param name="reader"></param>
    /// <param name="parameters"></param>
    /// <param name="nameBehavior"></param>
    /// <returns></returns>
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
    /// Checks for connection and transaction validity before execution. On error, logs query information.
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="QueryException"></exception>
    private static async Task<int> LoggedExecuteNonQueryAsync(NpgsqlCommand cmd, CancellationToken ct = default)
    {
        try
        {
            if (cmd.Connection == null)
                throw new InvalidOperationException("Command connection is null.");

            int rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

            if (cmd.Transaction != null && cmd.Transaction.Connection == null)
                throw new InvalidOperationException("Transaction was invalidated during execution.");

            return rowsAffected;
        }
        catch (Exception e)
        {
            throw new QueryException("Failed query", cmd.CommandText, e);
        }
    }

    /// <summary>
    /// Gets the constructor marked with SqlConstructor attribute
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static ConstructorInfo GetSqlConstructor<T>()
    {
        return typeof(T).GetConstructors().FirstOrDefault(c => Attribute.IsDefined(c, typeof(SqlConstructor)))
           ?? throw new InvalidOperationException($"No {nameof(SqlConstructor)} was defined in {typeof(T).FullName}");
    }

    /// <summary>
    /// Gets the constructor marked with SqlConstructor attribute as an SqlConstructor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
    /// Extracts property name. If SqlProperty is defined and found, and name is defined in SqlProperty, then that name is used. Else try to use the property's name.
    /// </summary>
    /// <param name="sqlAttribute"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    private static string GetPropertyName(SqlProperty? sqlAttribute, PropertyInfo property)
    {
        string name = string.Empty;

        if (sqlAttribute != null && sqlAttribute.Name != null)
        {
            name = sqlAttribute.Name;
        }
        else if (property != null && property.Name.Length > 0)
        {
            name = property.Name[0].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture);

            if (property.Name.Length > 1)
                name += property.Name.Substring(1);
        }

        return name;
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
    /// Creates parameters and placeholders for a property value
    /// </summary>
    /// <param name="property">The property to process</param>
    /// <param name="data">The data object containing the property value</param>
    /// <param name="paramIndex">Current parameter index (will be updated)</param>
    /// <returns>Tuple containing the parameters created and the placeholder string to use in query</returns>
    private static (List<NpgsqlParameter> parameters, string placeholder) CreatePropertyParameter<T>(PropertyInfo property, T data, ref int paramIndex)
    {
        var parameters = new List<NpgsqlParameter>();
        Type type = property.PropertyType;
        Type? underlyingType = Nullable.GetUnderlyingType(type);
        object? dataValue = property.GetValue(data);

        // Handle all other types 
        var paramValue = underlyingType != null && dataValue == null
            ? DBNull.Value
            : dataValue ?? GetDefaultValue(underlyingType ?? type) ?? DBNull.Value;

        var paramName = $"@p{paramIndex}";
        var param = new NpgsqlParameter(paramName, paramValue)
        {
            NpgsqlDbType = GetNpgsqlDbType(underlyingType ?? type)
        };

        parameters.Add(param);
        paramIndex++;

        return (parameters, paramName);

    }

    /// <summary>
    /// Maps C# types to their corresponding NpgsqlDbType
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get default value of type, if not possible to extract, then null.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private static object? GetDefaultValue(Type? t)
        => t?.IsValueType ?? false ? Activator.CreateInstance(t) : null;
}
