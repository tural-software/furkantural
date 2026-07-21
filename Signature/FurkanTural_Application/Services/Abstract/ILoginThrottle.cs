namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Parola deneme (brute-force) saldırılarına karşı login denemelerini sınırlar.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>
    /// Kullanıcı adı şu an kilitliyse kalan süreyi döndürür; kilitli değilse <c>null</c>.
    /// </summary>
    TimeSpan? GetRemainingLockout(string? username);

    /// <summary>
    /// Başarısız bir deneme kaydeder. Pencere içindeki deneme sayısı eşiği aşarsa kilit başlar.
    /// </summary>
    void RegisterFailure(string? username);

    /// <summary>
    /// Başarılı girişte sayacı sıfırlar.
    /// </summary>
    void Reset(string? username);
}
