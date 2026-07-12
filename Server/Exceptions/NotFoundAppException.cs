using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message, string errorCode = ErrorCodes.NotFound)
        : base(message, StatusCodes.Status404NotFound, errorCode) { }
}
