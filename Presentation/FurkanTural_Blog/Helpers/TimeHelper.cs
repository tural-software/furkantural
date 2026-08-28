namespace FurkanTural_Blog.Helpers;

public static class TimeHelper
{
    private static readonly TimeZoneInfo Tz = Resolve();

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
