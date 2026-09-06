using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Blog yazısı. Görselleri <see cref="BlogImage"/> taşır, kategorileri <see cref="BlogCategory"/> ara tablosu üzerinden <see cref="Category"/>'ye bağlanır.</summary>
public class Blog : BaseEntity
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    /// <summary>Yazının kalıcı adres parçası. Oluşturulurken başlıktan türetilir ve başlık sonradan değişse de <b>kendiliğinden değişmez</b>: adres bir kez paylaşıldıktan sonra onu değiştirmek dışarıdaki her bağlantıyı koparır. Elle düzeltmek mümkündür ama bilinçli bir karardır.<para>Tekildir ve silinmiş satırlar da tekil dizinde durmaya devam eder; silinen bir yazının adresi başkasına verilirse geri yükleme çakışır.</para></summary>
    public string? Slug { get; set; }
}
