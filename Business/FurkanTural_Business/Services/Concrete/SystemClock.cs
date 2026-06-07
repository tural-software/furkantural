using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// <see cref="IClock"/>'un sistem saatine dayalı uygulaması. Singleton kaydedilir.
/// Gösterim saat dilimi bir kez çözülür (Europe/Istanbul; Windows fallback: Turkey Standard Time).
/// </summary>
public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo TurkeyTimeZone = ResolveTurkeyTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public TimeZoneInfo DisplayTimeZone => TurkeyTimeZone;

    public DateTime ToDisplay(DateTime utc)
    {
        // Kind ne olursa olsun değeri UTC kabul et (kanonik kural), sonra Türkiye'ye çevir.
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, TurkeyTimeZone);
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Son çare: sabit +03:00 (Türkiye DST uygulamıyor).
        return TimeZoneInfo.CreateCustomTimeZone("TR+03", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}
