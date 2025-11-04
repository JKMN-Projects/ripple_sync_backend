namespace RippleSync.Domain.Users;

public abstract class UserToken
{
    public Guid Id { get; }
    public UserTokenType Type { get; protected set; }
    public string Token { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime ExpiresAt { get; protected set; }
    protected UserToken(Guid id, UserTokenType type, string token, DateTime createdAt, DateTime expiresAt)
    {
        Id = id;
        Type = type;
        Token = token;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }
}
