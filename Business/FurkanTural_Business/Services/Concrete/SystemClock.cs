using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// Gösterim saat dilimi süreç başına bir kez çözülür ve iki ad sırayla denenir: önce IANA kimliği
/// (<c>Europe/Istanbul</c>), sonra Windows karşılığı. İkisi de bulunamazsa sabit +03:00'lük bir dilim
/// üretilir; Türkiye yaz saati uygulamadığı için bu son çare pratikte doğru sonucu verir.
/// </summary>
public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo TurkeyTimeZone = ResolveTurkeyTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public TimeZoneInfo DisplayTimeZone => TurkeyTimeZone;

    public DateTime ToDisplay(DateTime utc)
    {
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
        return TimeZoneInfo.CreateCustomTimeZone("TR+03", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}