using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Yazının ikincil sınıflandırması; <see cref="Blog"/>'a <see cref="BlogTag"/> üzerinden çoğa-çok bağlanır.<para><see cref="Category"/>'den ayrıldığı yer sayı ve ağırlıktır, yapı değil: kategori azdır, renklidir ve yazının hangi alana ait olduğunu söyler; etiket çoktur, renksizdir ve yazının neye değindiğini söyler. Bu yüzden etiketin rengi yoktur — renk taşısaydı iki taksonomi görsel olarak eşitlenir ve okur hangisinin ana eksen olduğunu ayırt edemezdi.</para><para>Slug oluşturulurken addan üretilir ve ad sonradan değişse de kendiliğinden değişmez; gerekçesi <see cref="Category.Slug"/> ile aynıdır.</para></summary>
public class Tag : BaseEntity
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
}
