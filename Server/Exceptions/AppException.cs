using Server.Common.Errors;

namespace Server.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    protected AppException(string message, int statusCode, string errorCode) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Invalid credentials.", string errorCode = ErrorCodes.InvalidCredentials)
        : base(message, StatusCodes.Status401Unauthorized, errorCode) { }
}

public sealed class ValidationAppException : AppException
{
    public IEnumerable<string> Details { get; }

    public ValidationAppException(string message, IEnumerable<string>? details = null, string errorCode = ErrorCodes.ValidationFailed)
        : base(message, StatusCodes.Status400BadRequest, errorCode)
    {
        Details = details ?? [message];
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message, string errorCode = ErrorCodes.Forbidden)
        : base(message, StatusCodes.Status403Forbidden, errorCode) { }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message, string errorCode = ErrorCodes.NotFound)
        : base(message, StatusCodes.Status404NotFound, errorCode) { }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message, string errorCode = ErrorCodes.Conflict)
        : base(message, StatusCodes.Status409Conflict, errorCode) { }
}
