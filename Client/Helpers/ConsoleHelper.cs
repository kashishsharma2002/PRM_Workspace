namespace Client.Helpers;

public static class ConsoleHelper
{
    private const int BoxInnerWidth = 46;
    private static string? _pendingMessage = null;
    private static bool _isError = false;

    public static void PrintHeader(string title)
    {
        PrintBoxHeader(title);
    }

    public static void PrintBoxHeader(params string[] lines)
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Fallback for environments where Console.Clear is not supported
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        foreach (var line in lines)
        {
            Console.Write("║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(PadBoxLine(line));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("║");
        }
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        if (!string.IsNullOrEmpty(_pendingMessage))
        {
            Console.WriteLine();
            Console.ForegroundColor = _isError ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(_pendingMessage);
            Console.ResetColor();
            Console.WriteLine();
            _pendingMessage = null;
        }
    }

    public static void PrintDivider()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("──────────────────────────────────────────────");
        Console.ResetColor();
    }

    private static string PadBoxLine(string line)
    {
        if (line.Length > BoxInnerWidth)
            return line[..BoxInnerWidth];

        if (!line.StartsWith(" "))
        {
            var totalPadding = BoxInnerWidth - line.Length;
            var leftPadding = totalPadding / 2;
            var rightPadding = totalPadding - leftPadding;
            return new string(' ', leftPadding) + line + new string(' ', rightPadding);
        }

        return line.PadRight(BoxInnerWidth);
    }

    public static void PrintSuccess(string message)
    {
        _pendingMessage = $"✓  {message}";
        _isError = false;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{_pendingMessage}");
        Console.ResetColor();
    }

    public static void PrintError(string message)
    {
        _pendingMessage = $"⚠  {message}";
        _isError = true;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n{_pendingMessage}");
        Console.ResetColor();
    }

    public static string MapHealthLabel(string healthStatus) =>
        healthStatus.ToUpperInvariant() switch
        {
            "GREEN" => "ON TRACK",
            "AMBER" => "ATTENTION",
            "RED" => "AT RISK",
            _ => healthStatus
        };

    public static string MapHealthLabelWithEmoji(string healthStatus) =>
        healthStatus.ToUpperInvariant() switch
        {
            "GREEN" => "🟢 ON TRACK",
            "AMBER" => "🟡 ATTENTION",
            "RED" => "🔴 AT RISK",
            _ => healthStatus
        };
}
