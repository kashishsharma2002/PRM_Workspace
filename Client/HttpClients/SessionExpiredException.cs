namespace Client.HttpClients;

public sealed class SessionExpiredException : Exception
{
    public SessionExpiredException(string message) : base(message) { }
}
