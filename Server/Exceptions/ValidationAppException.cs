using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class ValidationAppException : AppException
{
    public IEnumerable<string> Details { get; }

    public ValidationAppException(string message, IEnumerable<string>? details = null, string errorCode = ErrorCodes.ValidationFailed)
        : base(message, StatusCodes.Status400BadRequest, errorCode)
    {
        Details = details ?? [message];
    }
}
