using RippleSync.Infrastructure.JukmanORM.ClassAttributes;

namespace RippleSync.Infrastructure.UserRepository.Entities;
internal class UserTokenEntity
{
    public Guid Id { get; }

    [SqlProperty(propName: "user_account_id")]
    public Guid UserAccountId { get; }

    [SqlProperty(propName: "token_type_id")]
    public int TokenTypeId { get; }

    [SqlProperty(propName: "token_value")]
    public string TokenValue { get; }

    [SqlProperty(propName: "created_at")]
    public DateTime CreatedAt { get; }

    [SqlProperty(propName: "expires_at")]
    public DateTime ExpiresAt { get; }


    //[SqlConstructor("ripple_sync", "user_token")]
    [SqlConstructor("public", "user_token")]
    public UserTokenEntity(Guid id, Guid user_account_id, int token_type_id, string token_value, DateTime created_at, DateTime expires_at)
    {
        Id = id;
        UserAccountId = user_account_id;
        TokenTypeId = token_type_id;
        TokenValue = token_value;
        CreatedAt = created_at;
        ExpiresAt = expires_at;
    }
}
