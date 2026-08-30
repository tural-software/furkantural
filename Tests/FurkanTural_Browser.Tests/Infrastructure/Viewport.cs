namespace FurkanTural_Browser.Tests.Infrastructure;

public sealed record Viewport(string Name, int Width, int Height)
{
    public static readonly Viewport Desktop = new("desktop-1400", 1400, 900);
    public static readonly Viewport Tablet = new("tablet-820", 820, 1180);
    public static readonly Viewport Phone = new("phone-390", 390, 844);

    public static readonly IReadOnlyList<Viewport> All = [Desktop, Tablet, Phone];

    public override string ToString() => Name;
}

public static class Themes
{
    public const string Dark = "dark";
    public const string Light = "light";

    public static readonly IReadOnlyList<string> Both = [Dark, Light];
}
