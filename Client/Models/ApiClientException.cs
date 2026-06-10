namespace Client.Models;

public sealed class ApiClientException : Exception
{
    public string? ErrorCode { get; }
    public int StatusCode { get; }

    public ApiClientException(string message, string? errorCode, int statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
