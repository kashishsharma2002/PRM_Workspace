namespace Server.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Invalid credentials.")
        : base(message, StatusCodes.Status401Unauthorized) { }
}

public sealed class ValidationAppException : AppException
{
    public IEnumerable<string> Details { get; }

    public ValidationAppException(string message, IEnumerable<string>? details = null)
        : base(message, StatusCodes.Status400BadRequest)
    {
        Details = details ?? [message];
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message)
        : base(message, StatusCodes.Status403Forbidden) { }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message)
        : base(message, StatusCodes.Status404NotFound) { }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message)
        : base(message, StatusCodes.Status409Conflict) { }
}
