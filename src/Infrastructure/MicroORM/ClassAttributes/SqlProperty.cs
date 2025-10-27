using System.Runtime.CompilerServices;

namespace RippleSync.Infrastructure.MicroORM.ClassAttributes;
[AttributeUsage(AttributeTargets.Property)]
internal class SqlProperty : Attribute
{
    internal string? Name { get; }
    internal QueryAction Action { get; }
    internal UpdateAction Update { get; }

    internal SqlProperty(QueryAction action = default, UpdateAction update = default, [CallerMemberName] string propName = "")
    {
        Name = null;

        if (propName.Length > 0)
        {
            Name = propName[0].ToString().ToLowerInvariant();
        }

        if (propName.Length > 1)
        {
            Name += propName[1..];
        }

        Action = action;
        Update = update;
    }
}

internal enum QueryAction
{
    NotDefined = 0,
    IgnoreInsert = 1,
}

internal enum UpdateAction
{
    NotDefined = 0,
    Ignore = 1,
    Where = 2,
}
