namespace FurkanTural_Admin.Models.Common;

/// <summary>
/// Bir API çağrısının sonucu: başarı + gerçek durum kodu + (varsa) API'nin kullanıcıya dönük
/// hata mesajı. Controller'lar bunu kullanarak gerçek durumu yüzeye çıkarır (401→login,
/// 4xx/5xx → gerçek mesaj) — eskiden her başarısızlık generic 500'e maskeleniyordu.
/// </summary>
public sealed record ApiCallResult(bool Success, int StatusCode, string? Message)
{
    public static ApiCallResult Ok() => new(true, 200, null);
    public static ApiCallResult Fail(int statusCode, string? message) => new(false, statusCode, message);
}