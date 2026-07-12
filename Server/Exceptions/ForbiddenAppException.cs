using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message, string errorCode = ErrorCodes.Forbidden)
        : base(message, StatusCodes.Status403Forbidden, errorCode) { }
}
