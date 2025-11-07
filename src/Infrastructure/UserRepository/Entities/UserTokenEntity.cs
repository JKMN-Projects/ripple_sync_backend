using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.UserRepository.Entities;

[method: SqlConstructor(tableName: "user_token")]
internal class UserTokenEntity(Guid id, Guid userAccountId, int tokenTypeId, string tokenValue, DateTime createdAt, DateTime expiresAt)
{

    [SqlProperty(update: UpdateAction.Where, isRecordIdentifier: true)]
    public Guid Id { get; } = id;

    [SqlProperty(update: UpdateAction.Where)]
    public Guid UserAccountId { get; } = userAccountId;
    public int TokenTypeId { get; } = tokenTypeId;
    public string TokenValue { get; } = tokenValue;
    public DateTime CreatedAt { get; } = createdAt;
    public DateTime ExpiresAt { get; } = expiresAt;
}
