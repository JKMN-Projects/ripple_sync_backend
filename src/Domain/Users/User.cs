namespace RippleSync.Domain.Users;
public class User(Guid id, string email)
{
    public Guid Id { get; set; } = id;
    public string Email { get; set; } = email;
}
