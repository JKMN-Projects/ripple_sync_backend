using RippleSync.Infrastructure.MicroORM.ClassAttributes;

namespace RippleSync.Infrastructure.UserRepository;
internal class UserEntity
{
    public Guid Id { get; }
    public string Email { get; }
    public string PasswordHash { get; }
    public string Salt { get; }


    [SqlConstructor("ripple_sync", "user")]
    public UserEntity(Guid id, string email, string password_hash, string salt)
    {
        Id = id;
        Email = email;
        PasswordHash = password_hash;
        Salt = salt;
    }
}
