using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using System.Reflection;

namespace RippleSync.Infrastructure.JukmanORM.Extensions;
public static partial class NpgsqlExtensions
{

    /// <summary>
    /// Flags for acquiring all instance properties (public and private) from a type and its base classes.
    /// </summary>
    private static readonly BindingFlags _acquirePropFlags = BindingFlags.FlattenHierarchy | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public enum WhereJoiner
    {
        OR,
        AND
    }

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
    public static async Task<IEnumerable<T>> QueryAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
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
                var row = ReadRow(reader, parameters, nameBehavior);

                if (!row.All(v => v == null || v == DBNull.Value))
                    dbValues.Add(row);
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
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
    {
        var constructor = GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters().Where(p => p.Name != null)];

        var dbValues = new object?[parameters.Length];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
                dbValues = ReadRow(reader, parameters, nameBehavior);
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        return dbValues.All(v => v == null || v == DBNull.Value) ?
            default
            : (T)constructor.Invoke(dbValues);
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
    public static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, string whereClause, object? param = null, CancellationToken ct = default)
        => await conn.SelectAsync<T>(null, whereClause, param, "", "", ct: ct);

    /// <summary>
    /// Executes a select query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="trans"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="param"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        IEnumerable<T> data;

        var query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

        data = await conn.QueryAsync<T>(query, param, trans, ct: ct);

        return data;
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
    public static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, string whereClause, object? param = null, CancellationToken ct = default)
        => await conn.SelectSingleOrDefaultAsync<T>(null, whereClause, param, "", "", ct: ct);

    /// <summary>
    /// Executes a select query and returns a single result of type T or Default of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="trans"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="param"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    public static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        var query = SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

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
    public static async Task<int> ExecuteAsync(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, CancellationToken ct = default)
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
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    public static async Task<int> InsertAsync<T>(this NpgsqlConnection conn, T data, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        IEnumerable<T> collection = [data];
        return await conn.InsertAsync(collection, trans, overwriteSchemaName, overwriteTableName, ct);
    }

    /// <summary>
    /// Inserts multiple records into the database table corresponding to Type T.
    /// Insertion is based on properties not marked with <see cref="QueryAction.IgnoreInsert"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    public static async Task<int> InsertAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var insert = $"INSERT INTO {schemaName}{tableName}(";
        var sqlValStart = ") VALUES ";

        List<string> colFields = [];

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();
            if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
                continue;

            var name = GetPropertyName(attribute, property);

            colFields.Add($"\"{name}\"");
        }

        var parameters = new List<NpgsqlParameter>();
        var rowValuePlaceholders = new List<string>();
        var paramIndex = 0;

        foreach (var data in datas)
        {
            var rowPlaceholders = new List<string>();

            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
                    continue;

                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);
                parameters.AddRange(propParams);
                rowPlaceholders.Add(placeholder);
            }

            rowValuePlaceholders.Add($"({string.Join(", ", rowPlaceholders)})");
        }

        var query = insert + string.Join(", ", colFields) + sqlValStart + string.Join(", ", rowValuePlaceholders);

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        var rowsAffected = await LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// update a single records in the database table corresponding to Type T.
    /// Update is based on properties not marked with <see cref="UpdateAction.Ignore"/>.
    /// Update uses on properties marked with <see cref="UpdateAction.Where"/> to defined Clause.
    /// Properties marked with Where will never be updated either, they're considered identifiers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="joiner">How to join the different wheres, default is AND</param>
    /// <param name="ct"></param>
    /// <returns>the number of affected rows</returns>
    public static async Task<int> UpdateAsync<T>(this NpgsqlConnection conn, T data, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        IEnumerable<T> collection = [data];
        return await conn.UpdateAsync(collection, trans, overwriteSchemaName, overwriteTableName, joiner, ct);
    }

    /// <summary>
    /// update multiple records in the database table corresponding to Type T.
    /// Update is based on properties not marked with <see cref="UpdateAction.Ignore"/>.
    /// Update uses properties marked with <see cref="UpdateAction.Where"/> to defined Clause.
    /// Properties marked with Where will never be updated either, they're considered identifiers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="joiner">How to join the different wheres, default is AND</param>
    /// <param name="ct"></param>
    /// <returns>the number of affected rows</returns>
    public static async Task<int> UpdateAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        List<string> updateFields = [];
        List<string> whereFields = [];

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            bool updateField = true;
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute != null && attribute.Update == UpdateAction.Ignore)
            {
                continue;
            }
            else if (attribute != null && attribute.Update == UpdateAction.Where)
            {
                updateField = false;
            }

            string name = GetPropertyName(attribute, property);

            if (updateField)
                updateFields.Add($"\"{name}\"");
            else
                whereFields.Add($"\"{name}\"");
        }

        IEnumerable<string> allFields = whereFields.Concat(updateFields);

        List<List<string>> updateColumns = [];

        var parameters = new List<NpgsqlParameter>();
        int paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowPlaceholders = [];

            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute != null && attribute.Update == UpdateAction.Ignore)
                    continue;

                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);
                parameters.AddRange(propParams);
                rowPlaceholders.Add(placeholder);
            }

            updateColumns.Add(rowPlaceholders);
        }

        string update = $"UPDATE {schemaName}{tableName} SET {string.Join(", ", updateFields.Select(u => $"{u} = b2.{u}"))}";
        string values = $" FROM (VALUES ({string.Join("), (", updateColumns.Select(u => string.Join(", ", u)))}";
        string valuesAsTable = $") ) AS b2({string.Join(", ", allFields)}) WHERE ";
        string where = string.Join($" {joiner} ", whereFields.Select(w => $"{tableName}.{w} = b2.{w}"));
        string query = update + values + valuesAsTable + where;

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        int rowsAffected = await LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Upserts a single record in the database table corresponding to Type T.
    /// Inserts if the record doesn't exist, updates if it does based on properties marked with <see cref="UpdateAction.Where"/>.
    /// </summary>
    public static async Task<int> UpsertAsync<T>(
        this NpgsqlConnection conn,
        T data,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        IEnumerable<T> collection = [data];
        return await conn.UpsertAsync(collection, trans, overwriteSchemaName, overwriteTableName, ct);
    }

    /// <summary>
    /// Upserts multiple records in the database table corresponding to Type T.
    /// Inserts if records don't exist, updates if they do based on properties marked with IsScopeIdentifier or IsRecordIdentifier.
    /// Uses PostgreSQL's ON CONFLICT clause.
    /// </summary>
    public static async Task<int> UpsertAsync<T>(
        this NpgsqlConnection conn,
        IEnumerable<T> datas,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();
        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        List<string> allFields = [];
        List<string> updateFields = [];
        List<string> conflictFields = [];

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update == UpdateAction.Ignore)
                continue;

            string name = GetPropertyName(attribute, property);
            allFields.Add($"\"{name}\"");

            if (attribute?.Update == UpdateAction.Where)
            { // Only use scope/record identifier for conflict resolution
                if (attribute.IsRecordIdentifier)
                {
                    conflictFields.Add($"\"{name}\"");
                }
                else if (attribute.IsScopeIdentifier)
                {
                    conflictFields.Add($"\"{name}\"");
                }
            }
            else
            { // Other fields and WHERE fields can still be updated
                updateFields.Add($"\"{name}\"");
            }
        }

        if (conflictFields.Count == 0)
            throw new InvalidOperationException($"Type {typeof(T).Name} must have at least one property marked with IsScopeIdentifier or IsRecordIdentifier for upsert logic");

        List<List<string>> valueRows = [];
        var parameters = new List<NpgsqlParameter>();
        int paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowPlaceholders = [];

            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute?.Update == UpdateAction.Ignore)
                    continue;

                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);
                parameters.AddRange(propParams);
                rowPlaceholders.Add(placeholder);
            }

            valueRows.Add(rowPlaceholders);
        }

        string insert = $"INSERT INTO {schemaName}{tableName} ({string.Join(", ", allFields)})";
        string values = $" VALUES ({string.Join("), (", valueRows.Select(row => string.Join(", ", row)))})";
        string conflict = $" ON CONFLICT ({string.Join(", ", conflictFields)})";
        string update = updateFields.Count > 0
            ? $" DO UPDATE SET {string.Join(", ", updateFields.Select(f => $"{f} = EXCLUDED.{f}"))}"
            : " DO NOTHING";

        string query = insert + values + conflict + update;

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
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="UpdateAction.Where"/> were defined on Type T</exception>
    public static async Task<int> RemoveAsync<T>(this NpgsqlConnection conn, T data, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        IEnumerable<T> collection = [data];
        return await conn.RemoveAsync(collection, trans, overwriteSchemaName, overwriteTableName, joiner, ct);
    }

    /// <summary>
    /// Removes multiple records from the database table corresponding to Type T.
    /// Deletion is based on properties marked with <see cref="UpdateAction.Where"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans">The transaction to use</param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="UpdateAction.Where"/> were defined on Type T</exception>
    public static async Task<int> RemoveAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var parameters = new List<NpgsqlParameter>();
        var whereConditions = new List<string>();
        var paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowConditions = [];
            foreach (var property in data?.GetType().GetProperties(_acquirePropFlags) ?? Enumerable.Empty<PropertyInfo>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute == null || attribute.Update != UpdateAction.Where)
                    continue;

                var name = GetPropertyName(attribute, property);
                var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);

                parameters.AddRange(propParams);
                rowConditions.Add($"\"{name}\" = {placeholder}");
            }

            if (rowConditions.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no properties marked with UpdateAction.Where for deletion.");

            whereConditions.Add($"({string.Join($" {joiner} ", rowConditions)})");
        }

        var query = $"DELETE FROM {schemaName}{tableName} WHERE {string.Join(" OR ", whereConditions)}";

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        var rowsAffected = await LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Synchronizes a single database record for a parent entity.
    /// Removes records that share the same parent identifier(s) but whose record identifier doesn't match.
    /// Then upserts the provided record.
    /// Requires one property marked with IsRecordIdentifier = true and at least one other WHERE property as parent identifier.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="data"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> SyncAsync<T>(
        this NpgsqlConnection conn,
        T data,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        IEnumerable<T> collection = [data];
        return await conn.SyncAsync(collection, trans, overwriteSchemaName, overwriteTableName, ct);
    }

    /// <summary>
    /// Synchronizes database records for a single parent entity.
    /// Removes records that share the same parent identifier(s) but whose record identifier is not in the collection.
    /// Then updates all records in the collection.
    /// Requires one property marked with IsRecordIdentifier = true and at least one other WHERE property as parent identifier.
    /// All items in the collection must share the same parent identifier values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<int> SyncAsync<T>(
        this NpgsqlConnection conn,
        IEnumerable<T> datas,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var dataList = datas.ToList();

        // Validate all items share same parent identifier values
        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();
        var parentProperties = GetParentProperties<T>();

        var firstParentValues = parentProperties.Select(p => p.Property.GetValue(dataList.First())).ToList();

        foreach (var item in dataList.Skip(1))
        {
            var itemParentValues = parentProperties.Select(p => p.Property.GetValue(item)).ToList();
            if (!firstParentValues.SequenceEqual(itemParentValues))
                throw new InvalidOperationException($"All items in collection must share the same parent identifier values for SyncAsync. Use SyncMultipleAsync for differing parents objects.");
        }

        return await SyncInternalAsync(conn, dataList, trans, overwriteSchemaName, overwriteTableName, ct);
    }

    /// <summary>
    /// Synchronizes database records for multiple parent entities.
    /// Groups items by parent identifier(s), then for each group removes records whose record identifier is not in the collection.
    /// Then updates all records in the collection.
    /// Requires one property marked with IsRecordIdentifier = true and at least one other WHERE property as parent identifier.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="datas"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<int> SyncMultipleAsync<T>(
        this NpgsqlConnection conn,
        IEnumerable<T> datas,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var dataList = datas.ToList();
        var parentProperties = GetParentProperties<T>();

        // Group by parent identifier values
        // splits it, for parentIds with same value, it stays together, children related to same parent
        //  if differs, it'll be split. children to different parents, need the different checks.
        var groupedData = dataList.GroupBy(item =>
        {
            var parentValues = parentProperties.Select(p => p.Property.GetValue(item)).ToArray();
            return string.Join("|", parentValues.Select(v => v?.ToString() ?? "null"));
        });

        int totalAffected = 0;

        foreach (var group in groupedData)
        {
            totalAffected += await SyncInternalAsync(conn, group.ToList(), trans, overwriteSchemaName, overwriteTableName, ct);
        }

        return totalAffected;
    }

    private static List<(PropertyInfo Property, string Name)> GetParentProperties<T>()
    {
        var parentProperties = new List<(PropertyInfo Property, string Name)>();

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update != UpdateAction.Where || attribute.IsRecordIdentifier)
                continue;

            var name = GetPropertyName(attribute, property);
            parentProperties.Add((property, name));
        }

        return parentProperties;
    }

    private static async Task<int> SyncInternalAsync<T>(
        NpgsqlConnection conn,
        IEnumerable<T> dataList,
        NpgsqlTransaction? trans,
        string overwriteSchemaName,
        string overwriteTableName,
        CancellationToken ct)
    {
        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();
        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);


        List<(PropertyInfo Property, string Name)> recordIdProperties = [];
        List<(PropertyInfo Property, string Name)> parentProperties = [];

        foreach (var property in typeof(T).GetProperties(_acquirePropFlags))
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update != UpdateAction.Where)
                continue;

            var name = GetPropertyName(attribute, property);

            if (attribute.IsRecordIdentifier)
            {
                recordIdProperties.Add((property, name));
            }
            else if (attribute.IsScopeIdentifier)
            {
                parentProperties.Add((property, name));
            }
            else
            {
                parentProperties.Add((property, name));
            }
        }

        if (recordIdProperties.Count == 0)
            throw new InvalidOperationException($"Type {typeof(T).Name} must have one property marked with IsRecordIdentifier = true for sync.");

        if (parentProperties.Count == 0)
            throw new InvalidOperationException($"Type {typeof(T).Name} must have at least one WHERE property as parent identifier for sync.");

        // Build condition for current record IDs (composite key support)
        var parameters = new List<NpgsqlParameter>();
        int paramIndex = 0;

        // Add parent conditions
        var parentConditions = new List<string>();
        var firstItem = dataList.First();
        foreach (var (property, name) in parentProperties)
        {
            var (propParams, placeholder) = CreatePropertyParameter(property, firstItem, ref paramIndex);
            parameters.AddRange(propParams);
            parentConditions.Add($"\"{name}\" = {placeholder}");
        }

        // For composite keys, build: NOT ((id1, id2) = ANY(VALUES (...)))
        // For single key, build: id <> ALL(ARRAY[...])

        string recordIdCondition;

        if (recordIdProperties.Count == 1)
        {
            // Single record identifier - build array approach
            var (property, name) = recordIdProperties[0];
            var currentIds = dataList.Select(d => property.GetValue(d)).ToArray();

            var idsParamName = $"@p{paramIndex}";
            var arrayDbType = NpgsqlDbType.Array | GetNpgsqlDbType(property.PropertyType);

            parameters.Add(new NpgsqlParameter(idsParamName, currentIds)
            {
                NpgsqlDbType = arrayDbType
            });

            recordIdCondition = $"\"{name}\" <> ALL({idsParamName})";
        }
        else
        {
            // Composite key - build row values
            var recordIdNames = string.Join(", ", recordIdProperties.Select(p => $"\"{p.Name}\""));
            var valuesList = new List<string>();

            foreach (var data in dataList)
            {
                var rowValues = new List<string>();
                foreach (var (property, _) in recordIdProperties)
                {
                    var (propParams, placeholder) = CreatePropertyParameter(property, data, ref paramIndex);
                    parameters.AddRange(propParams);
                    rowValues.Add(placeholder);
                }
                valuesList.Add($"({string.Join(", ", rowValues)})");
            }

            recordIdCondition = $"({recordIdNames}) NOT IN ({string.Join(", ", valuesList)})";
        }

        var deleteQuery = $"DELETE FROM {schemaName}{tableName} " +
                         $"WHERE {string.Join(" AND ", parentConditions)} " +
                         $"AND {recordIdCondition}";

        int rowsAffected = 0;

        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn, trans))
        {
            deleteCmd.Parameters.AddRange(parameters.ToArray());
            rowsAffected += await LoggedExecuteNonQueryAsync(deleteCmd, ct);
        }

        rowsAffected += await conn.UpsertAsync(dataList, trans, overwriteSchemaName, overwriteTableName, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Create Select Query to run in cmd
    /// </summary>
    /// <typeparam name="T"></typeparam>s
    /// <param name="whereClause"></param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string SelectQuery<T>(string whereClause = "", string overwriteSchemaName = "", string overwriteTableName = "")
    {
        var sqlConstructor = GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var where = whereClause;

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
        var values = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterName = parameters[i].Name;
            if (string.IsNullOrWhiteSpace(parameterName)) continue;

            var name = char.ToLower(parameterName[0], System.Globalization.CultureInfo.InvariantCulture).ToString();

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

            var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

            return cmd.Transaction != null && cmd.Transaction.Connection == null
                ? throw new InvalidOperationException("Transaction was invalidated during execution.")
                : rowsAffected;
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
        return typeof(T).GetConstructors().FirstOrDefault(c => Attribute.IsDefined(c, typeof(SqlConstructorAttribute)))
           ?? throw new InvalidOperationException($"No {nameof(SqlConstructorAttribute)} was defined in {typeof(T).FullName}");
    }

    /// <summary>
    /// Gets the constructor marked with SqlConstructor attribute as an SqlConstructor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static SqlConstructorAttribute GetConstructorOfTypeSqlConstructor<T>()
    {
        var constructor = GetSqlConstructor<T>();

        return constructor.GetCustomAttributes(false).OfType<SqlConstructorAttribute>().FirstOrDefault()
            ?? throw new InvalidOperationException($"No {nameof(SqlConstructorAttribute)} could be parsed from {typeof(T).FullName}");
    }

    /// <summary>
    /// Extrapolates Schema and Table name
    ///     Prioritizes overwrites, followed by <see cref="SqlConstructorAttribute"/>, then by class name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="overwriteTableName"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="ctorSqlAttribute"></param>
    /// <returns>schema and table name as tuple</returns>
    private static (string schemaName, string tableName) GetSchemaAndTableName<T>(string overwriteTableName, string overwriteSchemaName, SqlConstructorAttribute? ctorSqlAttribute = null)
    {
        var table = overwriteTableName;

        if (string.IsNullOrWhiteSpace(table))
            table = ctorSqlAttribute?.TableName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(table))
        {
            var name = typeof(T).Name;

            if (name.Length > 0)
                table = name[0].ToString().ToLowerInvariant();

            if (name.Length > 1)
                table += name[1..];
        }

        if (!string.IsNullOrWhiteSpace(table) && !table.Trim().EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
            table = "\"" + table.Trim() + "\"";

        var schema = overwriteSchemaName;

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
    private static string GetPropertyName(SqlPropertyAttribute? sqlAttribute, PropertyInfo property)
    {
        var name = string.Empty;

        if (sqlAttribute != null && sqlAttribute.Name != null)
        {
            name = sqlAttribute.Name;
        }
        else if (property != null && property.Name.Length > 0)
        {
            name = property.Name[0].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture);

            if (property.Name.Length > 1)
                name += property.Name[1..];
        }

        return name;
    }

    /// <summary>
    /// Adds <see cref="NpgsqlParameter"/>s to the cmd, from the parameter objects
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    private static void InsertParameters(this NpgsqlCommand cmd, object? param)
    {
        if (param != null)
        {
            var properties = param.GetType().GetProperties();

            foreach (var property in properties)
            {
                var type = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(type);
                var dataValue = property.GetValue(param);

                var paramValue = dataValue ?? GetDefaultValue(underlyingType ?? type) ?? DBNull.Value;
                var paramName = property.Name.StartsWith("@", StringComparison.InvariantCultureIgnoreCase) ? property.Name : $"@{property.Name}";

                SqlPropertyAttribute? attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                var dbType = attribute != null && attribute.DbType != NpgsqlDbType.Unknown
                    ? attribute.DbType
                    : GetNpgsqlDbType(underlyingType ?? type, dataValue);

                var parameter = new NpgsqlParameter(paramName, paramValue)
                {
                    NpgsqlDbType = dbType
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
        var type = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(type);
        var dataValue = property.GetValue(data);

        // Handle all other types 
        var paramValue = underlyingType != null && dataValue == null
            ? DBNull.Value
            : dataValue ?? GetDefaultValue(underlyingType ?? type) ?? DBNull.Value;

        var paramName = $"@p{paramIndex}";
        SqlPropertyAttribute? attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

        var dbType = attribute != null && attribute.DbType != NpgsqlDbType.Unknown
                    ? attribute.DbType
                    : GetNpgsqlDbType(underlyingType ?? type, dataValue);

        var param = new NpgsqlParameter(paramName, paramValue)
        {
            NpgsqlDbType = dbType
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
    private static NpgsqlDbType GetNpgsqlDbType(Type type, object? value = null)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        // Handle arrays
        if (actualType.IsArray)
        {
            var elementType = actualType.GetElementType()!;
            return NpgsqlDbType.Array | GetNpgsqlDbType(elementType);
        }

        // Handle generic collections (List<T>, IEnumerable<T>, etc.)
        if (actualType.IsGenericType)
        {
            var genericDef = actualType.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IList<>))
            {
                var elementType = actualType.GetGenericArguments()[0];
                return NpgsqlDbType.Array | GetNpgsqlDbType(elementType);
            }
        }

#pragma warning disable IDE0046 // Convert to conditional expression
        if (actualType == typeof(DateTime) && value is DateTime dt)
        {
            return dt.Kind == DateTimeKind.Unspecified
                ? NpgsqlDbType.Timestamp
                : NpgsqlDbType.TimestampTz;
        }
#pragma warning restore IDE0046 // Convert to conditional expression

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
            var t when t == typeof(DateTime) => NpgsqlDbType.TimestampTz, // Default to TimestampTz
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
