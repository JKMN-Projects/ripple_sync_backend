namespace RippleSync.Domain.Users;

public sealed class RefreshToken : UserToken
{
    private RefreshToken(Guid id, string token, DateTime createdAt, DateTime expiresAt)
        : base(id, UserTokenType.RefreshToken, token, createdAt, expiresAt)
    {
    }
    public static RefreshToken Create(string token, DateTime createdAt, DateTime expiresAt)
    {
        return new RefreshToken(
            default,
            token,
            createdAt,
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