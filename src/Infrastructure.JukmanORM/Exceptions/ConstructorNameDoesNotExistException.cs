namespace RippleSync.Infrastructure.JukmanORM.Exceptions;

public class ConstructorNameDoesNotExistException(string failedParameterName, string message, Exception? innerException) : Exception(message, innerException)
{
    public string FailedParameterName { get; private set; } = failedParameterName;

    public ConstructorNameDoesNotExistException(string failedParameterName, Exception? innerException)
        : this(failedParameterName, $"Parameter name {failedParameterName} from constructor doesn't exist in table", innerException) { }
}
