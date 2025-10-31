using RippleSync.Infrastructure.JukmanORM.Enums;
using System.Runtime.CompilerServices;

namespace RippleSync.Infrastructure.JukmanORM.ClassAttributes;
[AttributeUsage(AttributeTargets.Property)]
public class SqlProperty : Attribute
{
    public string? Name { get; }
    public QueryAction Action { get; }
    public UpdateAction Update { get; }

    public SqlProperty(QueryAction action = default, UpdateAction update = default, [CallerMemberName] string propName = "")
    {
        Name = null;

        if (propName.Length > 0)
            Name = propName[0].ToString().ToLowerInvariant();

        if (propName.Length > 1)
            Name += propName[1..];

        Action = action;
        Update = update;
    }
}
