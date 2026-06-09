namespace Client.Helpers;

public static class ConsoleHelper
{
    public static void PrintHeader(string title)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine($"  {title}");
        Console.WriteLine("==============================================");
    }

    public static void PrintDivider() =>
        Console.WriteLine("----------------------------------------------");

    public static void PrintSuccess(string message) =>
        Console.WriteLine($"\n[OK] {message}");

    public static void PrintError(string message) =>
        Console.WriteLine($"\n[ERROR] {message}");

    public static string MapHealthLabel(string healthStatus) =>
        healthStatus.ToUpperInvariant() switch
        {
            "GREEN" => "ON TRACK",
            "AMBER" => "ATTENTION",
            "RED" => "AT RISK",
            _ => healthStatus
        };
}
