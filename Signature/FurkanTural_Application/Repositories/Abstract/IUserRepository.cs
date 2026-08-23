using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Kullanıcıya özgü süzgeçsiz aramalar. Adlarındaki Admin, <see cref="IReadRepository{T}"/> sözleşmesindeki anlamıyla "hiçbir filtre uygulamaz" demektir, çağıranın admin olması gerektiği anlamına gelmez: buradaki asıl tüketici kayıt akışıdır ve anonim çalışır.<para>Süzgeçsiz olmaları zorunluluktur, tercih değil. Users tablosundaki Username ve Email tekil indeksleri yumuşak silmeye göre süzülmez, dolayısıyla silinmiş ya da pasif bir satırın kullanıcı adı hâlâ tutuludur. Kayıt öncesi kontrol global süzgeçten geçerse o satırı göremez, INSERT tekil indeks ihlaliyle patlar ve istemciye 500 döner. Bu iki metot kontrolün indeksle aynı şeyi görmesini sağlar.</para><para>Dönen kayıt izlenmez ve karşılaştırma veri tabanı harmanlamasına tabidir; öntanımlı SQL Server harmanlamasında büyük/küçük harf ayrımı yoktur.</para></summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameForAdminAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailForAdminAsync(string email, CancellationToken cancellationToken = default);
}
