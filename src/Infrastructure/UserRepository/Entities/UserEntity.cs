using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.UserRepository.Entities;

//[method: SqlConstructor(tableName: "user_account")]
//internal class UserEntity(Guid id, string email, string passwordHash, string salt, DateTime createdAt)
//{
//    [SqlProperty(update: UpdateAction.Where)]
//    public Guid Id { get; } = id;
//    public string Email { get; } = email;
//    public string PasswordHash { get; } = passwordHash;
//    public string Salt { get; } = salt;
//    public DateTime CreatedAt { get; } = createdAt;
//}


[method: SqlConstructor(tableName: "user_account")]
internal record UserEntity(
    [property: SqlProperty(update: UpdateAction.Where)] Guid Id,
    string Email,
    string PasswordHash,
    string Salt,
    DateTime CreatedAt);