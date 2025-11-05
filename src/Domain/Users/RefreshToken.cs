namespace RippleSync.Domain.Users;

public sealed class RefreshToken : UserToken
{
    private RefreshToken(Guid id, string token, DateTime createdAt, DateTime expiresAt)
        : base(id, UserTokenType.Refresh, token, createdAt, expiresAt)
    {
    }
    public static RefreshToken Create(string token, TimeProvider timeProvider, DateTime expiresAt)
    {
        return new RefreshToken(
            Guid.NewGuid(),
            token,
            timeProvider.GetUtcNow().UtcDateTime,
            expiresAt);
    }
    public static RefreshToken Reconstitute(Guid id, string token, DateTime createdAt, DateTime expiresAt)
    {
        return new RefreshToken(
            id,
            token,
            createdAt,
            expiresAt);
    }
}