namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Zamanın tek kaynağı. Servisler DateTime.UtcNow yerine bunu kullanır: hem testte saat
/// sabitlenebilsin hem de tüm damgalar aynı yerden gelsin diye. Saklanan ve API'den çıkan her tarih
/// UTC'dir; ToDisplay yalnızca sunum içindir ve girdinin Kind'ı ne olursa olsun değeri UTC kabul
/// eder, yani yerel saatle çağrılırsa sessizce kayar.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
    TimeZoneInfo DisplayTimeZone { get; }
    DateTime ToDisplay(DateTime utc);
}