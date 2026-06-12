using Client;
using Client.Common;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var clientSettings = config.GetSection("ClientSettings").Get<ClientSettings>()
    ?? throw new InvalidOperationException("ClientSettings configuration is missing.");

if (string.IsNullOrWhiteSpace(clientSettings.ServerBaseUrl))
    throw new InvalidOperationException("ClientSettings:ServerBaseUrl is required.");

await AppStarter.RunAsync(clientSettings.ServerBaseUrl);

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
