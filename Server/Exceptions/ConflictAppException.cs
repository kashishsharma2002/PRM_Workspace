using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message, string errorCode = ErrorCodes.Conflict)
        : base(message, StatusCodes.Status409Conflict, errorCode) { }
}
