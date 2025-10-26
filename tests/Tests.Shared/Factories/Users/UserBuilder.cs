
using RippleSync.Application.Common.Security;
using RippleSync.Domain.Users;
using System.Text;

namespace RippleSync.Tests.Shared.Factories.Users;

public class UserBuilder
{
    private IPasswordHasher _passwordHasher;

    private string _email = "default@example.com";
    private string _password = "Def4ultP@55";

    public UserBuilder(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }
    public UserBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public User Build()
    {
        byte[] saltBytes = _passwordHasher.GenerateSalt();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(_password);
        byte[] hashedPassword = _passwordHasher.Hash(passwordBytes, saltBytes);
        string saltString = Convert.ToBase64String(saltBytes);
        string hashedPasswordString = Convert.ToBase64String(hashedPassword);
        return User.Create(_email, hashedPasswordString, saltString);
    }
}
