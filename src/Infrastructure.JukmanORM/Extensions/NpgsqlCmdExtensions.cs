using Npgsql;
using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleSync.Infrastructure.JukmanORM.Extensions;
internal static partial class NpgsqlCmdExtensions
{
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
                var type = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(type);
                var dataValue = property.GetValue(param);

                // Materialize IEnumerable<T> to array for Npgsql compatibility
                dataValue = OrmHelper.MaterializeIfNeeded(dataValue, type);

                var paramValue = dataValue ?? OrmHelper.GetDefaultValue(underlyingType ?? type) ?? DBNull.Value;
                var paramName = property.Name.StartsWith("@", StringComparison.InvariantCultureIgnoreCase) ? property.Name : $"@{property.Name}";

                SqlPropertyAttribute? attribute = property.GetCustomAttributes(false).OfType<SqlPropertyAttribute>().FirstOrDefault();

                var dbType = attribute != null && attribute.DbType != NpgsqlDbType.Unknown
                    ? attribute.DbType
                    : OrmHelper.GetNpgsqlDbType(underlyingType ?? type, dataValue);

                var parameter = new NpgsqlParameter(paramName, paramValue)
                {
                    NpgsqlDbType = dbType
                };

                cmd.Parameters.Add(parameter);
            }
        }
    }
}
