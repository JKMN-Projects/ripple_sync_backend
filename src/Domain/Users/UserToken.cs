namespace RippleSync.Domain.Users;

public abstract class UserToken
{
    public Guid Id { get; }
    public UserTokenType Type { get; protected set; }
    public string Value { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime ExpiresAt { get; protected set; }

    protected UserToken(Guid id, UserTokenType type, string value, DateTime createdAt, DateTime expiresAt)
    {
        Id = id;
        Type = type;
        Value = value;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired(TimeProvider timeProvider) => timeProvider.GetUtcNow() >= ExpiresAt;
}
