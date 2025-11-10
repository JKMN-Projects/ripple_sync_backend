using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RippleSync.Infrastructure.JukmanORM;
internal class OrmHelper
{
    /// <summary>
    /// ORM setting for the naming to be generated. Defined name in <see cref="SqlPropertyAttribute"/> is still prioritized unchanged.
    /// </summary>
    public static SqlNamingConvention NamingConvention { get; set; } = SqlNamingConvention.SnakeCase;
    /// <summary>
    /// Flags for acquiring all instance properties (public and private) from a type and its base classes.
    /// </summary>
    private static readonly BindingFlags _acquirePropFlags = BindingFlags.FlattenHierarchy | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Create Select Query to run in cmd
    /// </summary>
    /// <typeparam name="T"></typeparam>s
    /// <param name="whereClause"></param>
    /// <param name="overwriteSchemaName">If defined, overrides the schema name</param>
    /// <param name="overwriteTableName">If defined, takes priority over assumed name from <typeparamref name="T"/> and over name potentially defined in <see cref="SqlConstructorAttribute"/></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static string SelectQuery<T>(string whereClause = "", string overwriteSchemaName = "", string overwriteTableName = "")
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
    internal static object?[] ReadRow(NpgsqlDataReader reader, ParameterInfo[] parameters, ParameterNameBehavior nameBehavior)
    {
        var values = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterName = parameters[i].Name;
            if (string.IsNullOrWhiteSpace(parameterName)) continue;

            var name = ToSqlName(parameterName);

            try
            {
                values[i] = reader[name] == DBNull.Value ? GetDefaultValue(parameters[i].ParameterType) : reader[name];
            }
            catch (IndexOutOfRangeException e)
            {
                switch (nameBehavior)
                {
                    case ParameterNameBehavior.FailOnNotFound:
                        throw new ConstructorNameDoesNotExistException(name, e);
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
    internal static async Task<int> LoggedExecuteNonQueryAsync(NpgsqlCommand cmd, CancellationToken ct = default)
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
    internal static ConstructorInfo GetSqlConstructor<T>()
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
    internal static SqlConstructorAttribute GetConstructorOfTypeSqlConstructor<T>()
    {
        var constructor = GetSqlConstructor<T>();

        return constructor.GetCustomAttributes(false).OfType<SqlConstructorAttribute>().FirstOrDefault()
            ?? throw new InvalidOperationException($"No {nameof(SqlConstructorAttribute)} could be parsed from {typeof(T).FullName}");
    }

    internal static List<(PropertyInfo Property, string Name)> GetParentProperties<T>()
    {
        var parentProperties = new List<(PropertyInfo Property, string Name)>();

        foreach (var property in ExtractProperties<T>())
        {
            var attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

            if (attribute?.Update != UpdateAction.Where || attribute.IsRecordIdentifier)
                continue;

            var name = GetPropertyName(attribute, property);
            parentProperties.Add((property, name));
        }

        return parentProperties;
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
    internal static (string schemaName, string tableName) GetSchemaAndTableName<T>(string overwriteTableName, string overwriteSchemaName, SqlConstructorAttribute? ctorSqlAttribute = null)
    {
        var table = overwriteTableName;

        if (string.IsNullOrWhiteSpace(table))
            table = ctorSqlAttribute?.TableName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(table))
        {
            table = ToSqlName(typeof(T).Name);
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
    internal static string GetPropertyName(SqlPropertyAttribute? sqlAttribute, PropertyInfo property)
    {
        var name = string.Empty;

        if (sqlAttribute != null && sqlAttribute.Name != null)
        {
            name = sqlAttribute.Name;
        }
        else if (property != null && property.Name.Length > 0)
        {
            name = ToSqlName(property.Name);
        }

        return name;
    }

    /// <summary>
    /// Creates parameters and placeholders for a property value
    /// </summary>
    /// <param name="property">The property to process</param>
    /// <param name="data">The data object containing the property value</param>
    /// <param name="paramIndex">Current parameter index (will be updated)</param>
    /// <returns>Tuple containing the parameters created and the placeholder string to use in query</returns>
    internal static (List<NpgsqlParameter> parameters, string placeholder) CreatePropertyParameter<T>(PropertyInfo property, T data, ref int paramIndex)
    {
        var parameters = new List<NpgsqlParameter>();
        var type = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(type);
        var dataValue = property.GetValue(data);

        // Materialize IEnumerable<T> to array for Npgsql compatibility
        dataValue = MaterializeIfNeeded(dataValue, type);

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
    /// Unified method to extract relevant properties in exact same way with same flags
    /// No deviation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static IEnumerable<PropertyInfo> ExtractProperties<T>()
        => typeof(T).GetProperties(_acquirePropFlags)
            .Where(p => !IsCompilerGeneratedProperty(p)) ?? [];


    /// <summary>
    /// Used for records flow, can find if a parameter is compiler generated, like EquilatorContract on records.
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    internal static bool IsCompilerGenerated(ParameterInfo parameter)
    {
        // Check if the parameter's declaring member (constructor) has CompilerGeneratedAttribute
        // or if the corresponding property has CompilerGeneratedAttribute
        var declaringType = parameter.Member.DeclaringType;
        if (declaringType == null) return false;

        var property = declaringType.GetProperty(parameter.Name!,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        return property?.GetCustomAttribute<CompilerGeneratedAttribute>() != null;
    }

    /// <summary>
    /// Used for records flow, can find if a property is compiler generated, like EquilatorContract on records.
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    internal static bool IsCompilerGeneratedProperty(PropertyInfo property)
        => property.GetCustomAttribute<CompilerGeneratedAttribute>() != null;

    /// <summary>
    /// Maps C# types to their corresponding NpgsqlDbType
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static NpgsqlDbType GetNpgsqlDbType(Type type, object? value = null)
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
    /// A check to see if the type is of primitive nature
    /// Uses .Nets built in plus more
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static bool IsPrimitiveOrSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || Nullable.GetUnderlyingType(type) != null && IsPrimitiveOrSimpleType(Nullable.GetUnderlyingType(type)!);
    }

    /// <summary>
    /// Converts IEnumerable<T> to T[] for Npgsql array parameter compatibility
    /// </summary>
    internal static object? MaterializeIfNeeded(object? dataValue, Type type)
    {
        if (dataValue == null) return null;

        // Skip if already array or implements IList (Npgsql compatible)
        if (type.IsArray || dataValue is System.Collections.IList)
            return dataValue;

        // Materialize pure IEnumerable<T> to preserve element type
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(elementType);
            return toArrayMethod.Invoke(null, [dataValue]);
        }

        return dataValue;
    }

    /// <summary>
    /// Converts the name to match the chosen <see cref="SqlNamingConvention"/> chosen
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static string ToSqlName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return NamingConvention switch
        {
            SqlNamingConvention.CamelCase => ToCamelCase(name),
            SqlNamingConvention.SnakeCase => ToSnakeCase(name),
            _ => name
        };
    }

    /// <summary>
    /// Method to convert name to CamelCase
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string ToCamelCase(string name)
    {
        if (name.Length == 0) return name;

        var result = char.ToLowerInvariant(name[0]).ToString();

        if (name.Length > 1)
            result += name[1..];

        return result;
    }

    /// <summary>
    /// Method to convert name to snake_case.
    /// Only creates _ on found uppercase letters
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string ToSnakeCase(string name)
    {
        if (name.Length == 0) return name;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                sb.Append(name[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get default value of type, if not possible to extract, then null.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    internal static object? GetDefaultValue(Type? t)
        => t?.IsValueType ?? false ? Activator.CreateInstance(t) : null;
}
