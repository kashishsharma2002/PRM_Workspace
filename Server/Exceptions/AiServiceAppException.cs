using Server.Common.Errors;

namespace Server.Exceptions;

public sealed class AiServiceAppException : AppException
{
    public AiServiceAppException(string message, string errorCode = ErrorCodes.LlmRequestFailed)
        : base(message, StatusCodes.Status503ServiceUnavailable, errorCode) { }
}
