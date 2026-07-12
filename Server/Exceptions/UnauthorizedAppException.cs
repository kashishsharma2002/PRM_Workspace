using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Invalid credentials.", string errorCode = ErrorCodes.InvalidCredentials)
        : base(message, StatusCodes.Status401Unauthorized, errorCode) { }
}
