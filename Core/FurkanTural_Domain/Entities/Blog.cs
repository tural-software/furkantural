using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Blog yazısı. Görselleri <see cref="BlogImage"/> taşır, kategorileri <see cref="BlogCategory"/> ara tablosu üzerinden <see cref="Category"/>'ye bağlanır.</summary>
public class Blog : BaseEntity
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    /// <summary>Yazının kalıcı adres parçası. Oluşturulurken başlıktan türetilir ve başlık sonradan değişse de <b>kendiliğinden değişmez</b>: adres bir kez paylaşıldıktan sonra onu değiştirmek dışarıdaki her bağlantıyı koparır. Elle düzeltmek mümkündür ama bilinçli bir karardır.<para>Tekildir ve silinmiş satırlar da tekil dizinde durmaya devam eder; silinen bir yazının adresi başkasına verilirse geri yükleme çakışır.</para></summary>
    public string? Slug { get; set; }

    /// <summary>Yazının okunma sayısı. Herkese açıktır ve yazı sayfasında görünür.<para>Sayaç sunucuya yalnızca tarayıcıdan gelen ayrı bir istekle artar, sayfanın çizilmesiyle değil. Ayrım bilinçli ve iki işi birden görüyor: JavaScript çalıştırmayan tarayıcılar sayıya girmez — arama motoru gezginlerinin çoğu böyledir — ve sayfayı yenileyen okur ikinci kez sayılmaz, çünkü tekrarı eleyen işaret tarayıcıda durur.</para><para>Değer artırılırken satır okunmaz; tek bir güncelleme deyimi çalışır, dolayısıyla aynı anda okuyan iki kişi birbirinin artışını ezemez.</para></summary>
    public int ViewCount { get; set; }
}
