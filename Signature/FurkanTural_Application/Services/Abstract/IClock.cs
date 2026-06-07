namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Uygulamanın tek kanonik zaman kaynağı. Tüm "şu an" çağrıları buradan geçer
/// (test edilebilirlik + tutarlılık). Kanonik değer her zaman UTC'dir; saat dilimine
/// çevirme yalnızca gösterim katmanında, <see cref="DisplayTimeZone"/> ile yapılır.
/// </summary>
public interface IClock
{
    /// <summary>Şimdiki an, UTC (Kind=Utc).</summary>
    DateTime UtcNow { get; }

    /// <summary>Şimdiki an, ofsetli UTC.</summary>
    DateTimeOffset UtcNowOffset { get; }

    /// <summary>Gösterim saat dilimi (Europe/Istanbul).</summary>
    TimeZoneInfo DisplayTimeZone { get; }

    /// <summary>Bir UTC zamanını gösterim saat dilimine (Türkiye) çevirir.</summary>
    DateTime ToDisplay(DateTime utc);
}
