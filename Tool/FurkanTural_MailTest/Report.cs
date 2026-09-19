namespace FurkanTural_MailTest;

internal static class Report
{
    public static int Failures { get; private set; }

    public static int Warnings { get; private set; }

    public static void Header(string message) => Write(ConsoleColor.DarkGray, message);

    public static void Section(string title)
    {
        Console.WriteLine();
        Write(ConsoleColor.Cyan, title);
    }

    public static void Ok(string message) => Write(ConsoleColor.Green, $"  [ OK ]  {message}");

    public static void Fail(string message)
    {
        Failures++;
        Write(ConsoleColor.Red, $"  [HATA]  {message}");
    }

    public static void Warn(string message)
    {
        Warnings++;
        Write(ConsoleColor.Yellow, $"  [UYARI] {message}");
    }

    public static void Info(string message) => Console.WriteLine($"          {message}");

    public static void Trace(string message) => Write(ConsoleColor.DarkGray, $"          {message}");

    public static void Summary(ConsoleColor color, string message)
    {
        Console.WriteLine();
        Write(color, message);
    }

    private static void Write(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
