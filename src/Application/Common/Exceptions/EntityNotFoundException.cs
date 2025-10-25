
using System.Linq.Expressions;

namespace RippleSync.Application.Common.Exceptions;

public class EntityNotFoundException : Exception
{
    public string? EntityType { get; }
    public object? Key { get; }
    public string? KeyName { get; }

    public EntityNotFoundException(string message) : base(message)
    {
    }

    public EntityNotFoundException(string entityType, object key) : this($"Entity of type '{entityType}' with key '{key}' not found.")
    {
        EntityType = entityType;
        Key = key;
    }

    public EntityNotFoundException(string entityType, object key, string keyName) : this($"Entity of type '{entityType}' with {keyName} '{key}' not found.")
    {
        EntityType = entityType;
        Key = key;
        KeyName = keyName;
    }

    public static EntityNotFoundException ForEntity<TEntity>(object key, string? keyName)
        where TEntity : class
    {
        string entityType = typeof(TEntity).Name;

        if (keyName is null)
        {
            return new EntityNotFoundException(entityType, key);
        }

        return new EntityNotFoundException(entityType, key, keyName);
    }
    public static EntityNotFoundException ForEntity<TEntity>(object key)
        where TEntity : class
        => ForEntity<TEntity>(key, null);

    public static EntityNotFoundException ForEntity<TEntity, TProp>(object key, Expression<Func<TEntity, TProp>> keyName)
        where TEntity : class
    {
        if (keyName.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Expression must be a member expression.", nameof(keyName));
        }
        return ForEntity<TEntity>(key, memberExpression.Member.Name);
    }
}
