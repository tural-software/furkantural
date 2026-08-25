using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Aboneye özgü süzgeçsiz arama. Adındaki Admin, <see cref="IReadRepository{T}"/> sözleşmesindeki anlamıyla "hiçbir filtre uygulamaz" demektir, çağıranın admin olması gerektiği anlamına gelmez: asıl tüketici abonelik akışıdır ve anonim çalışır.<para>Zorunluluktur, tercih değil: Subscribers tablosundaki Email tekil indeksi yumuşak silmeye göre süzülmez, dolayısıyla abonelikten çıkmış bir adres indekste hâlâ tutuludur. Kontrol global süzgeçten geçerse o satırı göremez, INSERT tekil indekse takılır ve yeniden abone olmak isteyen kişi kendi eski kaydı yüzünden reddedilir. Bu metot kontrolün indeksle aynı şeyi görmesini sağlar (aynı tuzak için bkz. <see cref="IUserRepository"/>).</para><para>Dönen kayıt izlenmez ve karşılaştırma veri tabanı harmanlamasına tabidir; öntanımlı SQL Server harmanlamasında büyük/küçük harf ayrımı yoktur.</para></summary>
public interface ISubscriberRepository : IRepository<Subscriber>
{
    Task<Subscriber?> GetByEmailForAdminAsync(string email, CancellationToken cancellationToken = default);
}
