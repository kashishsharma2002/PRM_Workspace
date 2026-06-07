using Client.Helpers;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var serverBaseUrl = config["ServerBaseUrl"] ?? "https://localhost:5001";

ConsoleHelper.PrintHeader("PRM Client — Phase 0");
Console.WriteLine($"Server: {serverBaseUrl}");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
