using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Pasife alınmış bir hesabın e-posta doğrulamasıyla geri açılması. Jeton kriptografik rastgeleden üretilir; düz hâli yalnızca çağırana döner ve oradan e-postaya girer, tabloya SHA-256 türevi yazılır. Veri tabanını okuyabilen biri jetonu geri üretemez, dolayısıyla saklanan değer tek başına bir hesabı açmaya yetmez.<para><see cref="IssueAsync"/>'in döndürdüğü metin bir kimlik bilgisidir: hiçbir uçtan yanıt olarak dışarı verilmemeli, kayda yazılmamalıdır. Çağıranın tek işi onu doğrulama bağlantısına koyup kayıtlı adrese göndermektir.</para><para>Jeton tek kullanımlıktır ve 24 saat geçerlidir. Aynı hesap için birden çok jeton aynı anda açık olabilir — yeni bir istek öncekileri geçersizleştirmez, çünkü ikisi de aynı kişiye gitmiştir ve her biri zaten tek kullanımlık ve süreliyken bunu yapmak yalnızca geç ulaşan postayı işe yaramaz hâle getirirdi.</para><para><see cref="ConsumeAsync"/> hesabı yalnızca aktifleştirir, oturum açmaz; jetonu taşıyan kişiye token verilmez, kullanıcı normal giriş akışına yönlendirilir. Silinmiş hesap bu yolla açılamaz — silme admin kararıdır ve geri alınması da admin işidir.</para></summary>
public interface IAccountActivationService
{
    Task<Result<string>> IssueAsync(int userId, string triggerSource, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result> ConsumeAsync(string? token, CancellationToken cancellationToken = default);
}
