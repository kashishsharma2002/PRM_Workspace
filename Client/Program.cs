using Client;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var serverBaseUrl = config["ServerBaseUrl"] ?? "https://localhost:5001";

await AppStarter.RunAsync(serverBaseUrl);

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
