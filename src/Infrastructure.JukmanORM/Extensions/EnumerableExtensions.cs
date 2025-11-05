namespace RippleSync.Infrastructure.JukmanORM.Extensions;
public static partial class EnumerableExtensions
{
    public static bool NullOrEmpty<T>(this IEnumerable<T>? data)
        => data == null || !data.Any();
}
