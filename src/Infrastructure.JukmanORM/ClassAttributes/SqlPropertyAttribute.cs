using NpgsqlTypes;
using RippleSync.Infrastructure.JukmanORM.Enums;
using System.Runtime.CompilerServices;

namespace RippleSync.Infrastructure.JukmanORM.ClassAttributes;
[AttributeUsage(AttributeTargets.Property)]
public class SqlPropertyAttribute : Attribute
{
    public string? Name { get; }
    public QueryAction Action { get; }
    public UpdateAction Update { get; }
    public NpgsqlDbType DbType { get; }
    public bool IsRecordIdentifier { get; }
    public bool IsScopeIdentifier { get; }

    public SqlPropertyAttribute(QueryAction action = default, UpdateAction update = default, NpgsqlDbType dbType = NpgsqlDbType.Unknown, bool isRecordIdentifier = false, bool isScopeIdentifier = false, [CallerMemberName] string propName = "")
    {
        Name = null;

        if (propName.Length > 0)
            Name = propName[0].ToString().ToLowerInvariant();

        if (propName.Length > 1)
            Name += propName[1..];

        Action = action;
        Update = update;
        DbType = dbType;
        IsRecordIdentifier = isRecordIdentifier;
        IsScopeIdentifier = isScopeIdentifier;
    }
}
