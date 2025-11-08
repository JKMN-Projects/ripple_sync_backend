using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using System.Reflection;

namespace RippleSync.Infrastructure.JukmanORM.Extensions;

public static partial class NpgsqlConnExtensions
{
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
    /// <exception cref="QueryException"></exception>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
    {
        var targetType = typeof(T);

        // Handle primitive/simple types
        if (OrmHelper.IsPrimitiveOrSimpleType(targetType))
        {
            return await conn.QueryPrimitiveAsync<T>(query, param, trans, ct);
        }

        var constructor = OrmHelper.GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters()
                                                        .Where(p => p.Name != null && !OrmHelper.IsCompilerGenerated(p))];

        List<object?[]> dbValues = [];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var row = OrmHelper.ReadRow(reader, parameters, nameBehavior);

                if (!row.All(v => v == null || v == DBNull.Value))
                    dbValues.Add(row);
            }
        }
        catch (ConstructorNameDoesNotExistException)
        { throw; }
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
    /// <exception cref="QueryException"></exception>
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, ParameterNameBehavior nameBehavior = ParameterNameBehavior.FailOnNotFound, CancellationToken ct = default)
    {
        var targetType = typeof(T);

        // Handle primitive/simple types
        if (OrmHelper.IsPrimitiveOrSimpleType(targetType))
        {
            return await conn.QueryPrimitiveSingleOrDefaultAsync<T>(query, param, trans, ct);
        }

        var constructor = OrmHelper.GetSqlConstructor<T>();

        ParameterInfo[] parameters = [.. constructor.GetParameters()
                                                        .Where(p => p.Name != null && !OrmHelper.IsCompilerGenerated(p))];

        var dbValues = new object?[parameters.Length];

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
                dbValues = OrmHelper.ReadRow(reader, parameters, nameBehavior);
        }
        catch (Exception e)
        { throw new QueryException("Querying to class error", query, e); }

        return dbValues.All(v => v == null || v == DBNull.Value) ?
            default
            : (T)constructor.Invoke(dbValues);
    }

    /// <summary>
    /// Internal querying method, gets the result as multiple primitive datatype
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="query"></param>
    /// <param name="param"></param>
    /// <param name="trans"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="QueryException"></exception>
    private static async Task<IEnumerable<T>> QueryPrimitiveAsync<T>(this NpgsqlConnection conn, string query, object? param, NpgsqlTransaction? trans, CancellationToken ct)
    {
        var results = new List<T>();

        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var value = reader.GetValue(0);
                if (value != null && value != DBNull.Value)
                {
                    results.Add((T)value);
                }
            }
        }
        catch (Exception e)
        {
            throw new QueryException("Querying primitive type error", query, e);
        }

        return results;
    }

    /// <summary>
    /// Internal querying method, gets the primitive datatype or default
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="query"></param>
    /// <param name="param"></param>
    /// <param name="trans"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="QueryException"></exception>
    private static async Task<T?> QueryPrimitiveSingleOrDefaultAsync<T>(this NpgsqlConnection conn, string query, object? param, NpgsqlTransaction? trans, CancellationToken ct)
    {
        try
        {
            using var cmd = new NpgsqlCommand(query, conn, trans);
            cmd.InsertParameters(param);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            if (await reader.ReadAsync(ct))
            {
                var value = reader.GetValue(0);

                if (await reader.ReadAsync(ct))
                {
                    throw new InvalidOperationException("Sequence contains more than one element");
                }

                if (value != null && value != DBNull.Value)
                {
                    return (T)value;
                }
            }
        }
        catch (Exception e)
        {
            throw new QueryException("Querying primitive type error", query, e);
        }

        return default;
    }

    /// <summary>
    /// Executes a select query and returns the result as an enumerable of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="whereClause">Add a where clause, "WHERE" being optional</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="QueryException"></exception>
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
    /// <exception cref="QueryException"></exception>
    public static async Task<IEnumerable<T>> SelectAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        IEnumerable<T> data;

        var query = OrmHelper.SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

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
    /// <exception cref="QueryException"></exception>
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
    /// <exception cref="QueryException"></exception>
    public static async Task<T?> SelectSingleOrDefaultAsync<T>(this NpgsqlConnection conn, NpgsqlTransaction? trans = null, string whereClause = "", object? param = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        var query = OrmHelper.SelectQuery<T>(whereClause, overwriteSchemaName, overwriteTableName);

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
    /// <exception cref="QueryException"></exception>
    public static async Task<int> ExecuteAsync(this NpgsqlConnection conn, string query, object? param = null, NpgsqlTransaction? trans = null, CancellationToken ct = default)
    {
        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.InsertParameters(param);

        return await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct); ;
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
    /// <exception cref="QueryException"></exception>
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
    /// <exception cref="QueryException"></exception>
    public static async Task<int> InsertAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var insert = $"INSERT INTO {schemaName}{tableName}(";
        var sqlValStart = ") VALUES ";

        List<string> colFields = [];

        foreach (var property in OrmHelper.ExtractProperties<T>())
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();
            if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
                continue;

            var name = OrmHelper.GetPropertyName(attribute, property);

            colFields.Add($"\"{name}\"");
        }

        var parameters = new List<NpgsqlParameter>();
        var rowValuePlaceholders = new List<string>();
        var paramIndex = 0;

        foreach (var data in datas)
        {
            var rowPlaceholders = new List<string>();

            foreach (var property in OrmHelper.ExtractProperties<T>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute != null && attribute.Action == QueryAction.IgnoreInsert)
                    continue;

                var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, data, ref paramIndex);

                parameters.AddRange(propParams);
                rowPlaceholders.Add(placeholder);
            }

            rowValuePlaceholders.Add($"({string.Join(", ", rowPlaceholders)})");
        }

        var query = insert + string.Join(", ", colFields) + sqlValStart + string.Join(", ", rowValuePlaceholders);

        using var cmd = new NpgsqlCommand(query, conn, trans);
        cmd.Parameters.AddRange(parameters.ToArray());

        var rowsAffected = await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct);

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
    /// <exception cref="QueryException"></exception>
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
    /// <exception cref="QueryException"></exception>
    public static async Task<int> UpdateAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        List<string> updateFields = [];
        List<string> whereFields = [];

        foreach (var property in OrmHelper.ExtractProperties<T>())
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

            string name = OrmHelper.GetPropertyName(attribute, property);

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

            foreach (var property in OrmHelper.ExtractProperties<T>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute != null && attribute.Update == UpdateAction.Ignore)
                    continue;

                var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, data, ref paramIndex);

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

        int rowsAffected = await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
    }

    /// <summary>
    /// Upserts a single record in the database table corresponding to Type T.
    /// Inserts if the record doesn't exist, updates if it does based on properties marked with <see cref="UpdateAction.Where"/>.
    /// </summary>
    /// <exception cref="QueryException"></exception>
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
    /// <exception cref="QueryException"></exception>
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

        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();
        var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        List<string> allFields = [];
        List<string> updateFields = [];
        List<string> conflictFields = [];

        foreach (var property in OrmHelper.ExtractProperties<T>())
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update == UpdateAction.Ignore)
                continue;

            string name = OrmHelper.GetPropertyName(attribute, property);
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

            foreach (var property in OrmHelper.ExtractProperties<T>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute?.Update == UpdateAction.Ignore)
                    continue;

                var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, data, ref paramIndex);
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

        int rowsAffected = await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct);

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
    /// <exception cref="QueryException"></exception>
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
    /// 
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="UpdateAction.Where"/> were defined on Type T</exception>
    /// <exception cref="QueryException"></exception>
    public static async Task<int> RemoveAsync<T>(this NpgsqlConnection conn, IEnumerable<T> datas, NpgsqlTransaction? trans = null, string overwriteSchemaName = "", string overwriteTableName = "", WhereJoiner joiner = WhereJoiner.AND, CancellationToken ct = default)
    {
        if (datas.NullOrEmpty())
            return 0;

        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();

        var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

        var parameters = new List<NpgsqlParameter>();
        var whereConditions = new List<string>();
        var paramIndex = 0;

        foreach (var data in datas)
        {
            List<string> rowConditions = [];
            foreach (var property in OrmHelper.ExtractProperties<T>())
            {
                var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                if (attribute == null || attribute.Update != UpdateAction.Where)
                    continue;

                var name = OrmHelper.GetPropertyName(attribute, property);
                var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, data, ref paramIndex);

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

        var rowsAffected = await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct);

        return rowsAffected;
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
    /// <exception cref="QueryException"></exception>
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
        var parentProperties = OrmHelper.GetParentProperties<T>();

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
            var firstItem = group.First();
            var parentIdProperties = parentProperties.ToDictionary(
                p => p.Property.Name,
                p => p.Property.GetValue(firstItem)
            );

            var parentRunTimeObject = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
            foreach (var kvp in parentIdProperties)
            {
                parentRunTimeObject[kvp.Key] = kvp.Value;
            }

            totalAffected += await SyncAsync(conn, group.ToList(), parentIdentifiers: parentRunTimeObject, trans, overwriteSchemaName, overwriteTableName, ct);
        }

        return totalAffected;
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
    /// <param name="parentIdentifiers">Should be supplied, incase children objects are null or empty</param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="QueryException"></exception>
    public static async Task<int> SyncAsync<T>(
        this NpgsqlConnection conn,
        IEnumerable<T?> datas,
        object? parentIdentifiers = null,
        NpgsqlTransaction? trans = null,
        string overwriteSchemaName = "",
        string overwriteTableName = "",
        CancellationToken ct = default)
    {
        var dataList = datas?.ToList() ?? [];

        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();
        var parentProperties = OrmHelper.GetParentProperties<T>();

        if (dataList.Count == 0)
        {
            if (parentIdentifiers == null)
                return 0;

            // Delete all children for the given parent
            var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);

            var conditions = new List<string>();
            var parameters = new List<NpgsqlParameter>();
            int paramIndex = 0;

            var parentIdType = parentIdentifiers.GetType();

            foreach (var (property, name) in parentProperties)
            {
                var value = parentIdType.GetProperty(property.Name)?.GetValue(parentIdentifiers);
                var paramName = $"@p{paramIndex++}";
                conditions.Add($"\"{name}\" = {paramName}");
                parameters.Add(new NpgsqlParameter(paramName, value ?? DBNull.Value));
            }

            string deleteQuery = $"DELETE FROM {schemaName}{tableName} WHERE {string.Join(" AND ", conditions)}";

            using var cmd = new NpgsqlCommand(deleteQuery, conn, trans);
            cmd.Parameters.AddRange(parameters.ToArray());
            return await OrmHelper.LoggedExecuteNonQueryAsync(cmd, ct);
        }

        // Validate all items share same parent identifier values
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
    /// Internal method for the SyncAsync flow
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <param name="dataList"></param>
    /// <param name="trans"></param>
    /// <param name="overwriteSchemaName"></param>
    /// <param name="overwriteTableName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<int> SyncInternalAsync<T>(
        NpgsqlConnection conn,
        IEnumerable<T> dataList,
        NpgsqlTransaction? trans,
        string overwriteSchemaName,
        string overwriteTableName,
        CancellationToken ct)
    {
        var sqlConstructor = OrmHelper.GetConstructorOfTypeSqlConstructor<T>();
        var (schemaName, tableName) = OrmHelper.GetSchemaAndTableName<T>(overwriteSchemaName, overwriteTableName, sqlConstructor);


        List<(PropertyInfo Property, string Name)> recordIdProperties = [];
        List<(PropertyInfo Property, string Name)> parentProperties = [];

        foreach (var property in OrmHelper.ExtractProperties<T>())
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update != UpdateAction.Where)
                continue;

            var name = OrmHelper.GetPropertyName(attribute, property);

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
            var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, firstItem, ref paramIndex);
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
            var arrayDbType = NpgsqlDbType.Array | OrmHelper.GetNpgsqlDbType(property.PropertyType);

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
                    var (propParams, placeholder) = OrmHelper.CreatePropertyParameter(property, data, ref paramIndex);
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
            rowsAffected += await OrmHelper.LoggedExecuteNonQueryAsync(deleteCmd, ct);
        }

        rowsAffected += await conn.UpsertAsync(dataList, trans, overwriteSchemaName, overwriteTableName, ct);

        return rowsAffected;
    }
}
