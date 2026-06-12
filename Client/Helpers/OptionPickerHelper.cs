namespace Client.Helpers;

public static class OptionPickerHelper
{
    public static string? PickFromList(string label, IReadOnlyList<string> values, Func<string, string> getDisplayName)
    {
        if (values.Count == 0)
            return null;

        Console.WriteLine(label);
        for (var i = 0; i < values.Count; i++)
            Console.WriteLine($"  ({i + 1}) {getDisplayName(values[i])}");

        Console.Write($"Select [1-{values.Count}]: ");
        var choice = Console.ReadLine()?.Trim();
        if (!int.TryParse(choice, out var index) || index < 1 || index > values.Count)
            return null;

        return values[index - 1];
    }
}
