namespace RippleSync.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorize access to resource ") : base(message)
    {

    }
}
