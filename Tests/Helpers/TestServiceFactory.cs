using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Helpers;

public static class TestServiceFactory
{
    public static ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;
}
