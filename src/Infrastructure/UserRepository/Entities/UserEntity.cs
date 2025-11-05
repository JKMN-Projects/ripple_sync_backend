using RippleSync.Infrastructure.JukmanORM.ClassAttributes;
using RippleSync.Infrastructure.JukmanORM.Enums;

namespace RippleSync.Infrastructure.UserRepository.Entities;
internal class UserEntity
{
    [SqlProperty(update: UpdateAction.Where)]
    public Guid Id { get; }

    public string Email { get; }

    [SqlProperty(propName: "password_hash")]
    public string PasswordHash { get; }

    public string Salt { get; }

    [SqlProperty(propName: "created_At")]
    public DateTime CreatedAt { get; }


    //[SqlConstructor("ripple_sync", "user_account")]
    [SqlConstructor("public", "user_account")]
    public UserEntity(Guid id, string email, string password_hash, string salt, DateTime created_at)
    {
        Id = id;
        Email = email;
        PasswordHash = password_hash;
        Salt = salt;
        CreatedAt = created_at;
    }
}
