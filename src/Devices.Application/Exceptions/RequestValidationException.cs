namespace Devices.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message)
        : base(message)
    {
    }
}
