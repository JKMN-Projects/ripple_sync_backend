
namespace RippleSync.Application.Users.Exceptions;
public class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException() : base("Email is already in use.") { }
    public EmailAlreadyInUseException(string email) : base($"Email '{email}' is already in use.")
    {
        Data.Add("Email", email);
    }
}
