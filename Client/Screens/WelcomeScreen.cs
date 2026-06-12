using Client.Helpers;

namespace Client.Screens;

public enum WelcomeChoice
{
    Login,
    Exit
}

public static class WelcomeScreen
{
    public static WelcomeChoice Run()
    {
        while (true)
        {
            ConsoleHelper.PrintBoxHeader(
                "    PROJECT & RESOURCE MANAGEMENT TOOL",
                "    Learn & Code — Final Project");
            Console.WriteLine();
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Exit");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    return WelcomeChoice.Login;
                case "2":
                    return WelcomeChoice.Exit;
                default:
                    ConsoleHelper.PrintError("Invalid option.");
                    Console.WriteLine();
                    break;
            }
        }
    }
}
