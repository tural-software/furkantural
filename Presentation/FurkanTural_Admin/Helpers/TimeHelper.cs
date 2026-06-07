namespace FurkanTural_Admin.Helpers;

/// <summary>
/// Sunucu render'ında UTC zamanları Europe/Istanbul'a çevirir (sunucu saat diliminden bağımsız).
/// API tüm tarihleri UTC 'Z' olarak döndürür; burada gelen değer her zaman UTC kabul edilir.
/// </summary>
public static class TimeHelper
{
    private static readonly TimeZoneInfo Tz = Resolve();

    public static DateTime ToIstanbul(DateTime value)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), Tz);

    public static DateTime NowIstanbul
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("TR+03", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}
