namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Giriş denemesi sınırlayıcı. Metotlara yalnızca kullanıcı adı verilse de kilit anahtarı kullanıcı
/// adı ile istemci IP'sinin bileşimidir: aksi hâlde bilinen bir hesabı kasten kilitleyip meşru
/// sahibini dışarıda bırakmak mümkün olurdu. GetRemainingLockout null dönerse kilit yoktur, süre
/// dönerse çağıran parolayı hiç doğrulamadan reddetmelidir — kilitliyken hash hesaplamak boşa yük
/// olur. Var olmayan kullanıcı adları için de sayaç işletilir, çünkü kilit yanıtı hesabın varlığını
/// ele vermemelidir. Sayaçlar bellek içidir ve süreç yeniden başlarsa sıfırlanır.
/// </summary>
public interface ILoginThrottle
{
    TimeSpan? GetRemainingLockout(string? username);
    void RegisterFailure(string? username);
    void Reset(string? username);
}