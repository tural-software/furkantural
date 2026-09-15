namespace FurkanTural_Business.Helpers;

public static class SecurityStamps
{
    public static string New() => Guid.NewGuid().ToString("N");
}
