namespace FurkanTural_Blog.Models;

/// <summary>Yazının ikincil sınıflandırması — yazı sayfasındaki çipler, etiket sayfası ve etiket bulutu için.<para>Kategoriden farkı rengi olmamasıdır ve bu bilinçlidir: renk taşısaydı iki taksonomi görsel olarak eşitlenir, okur hangisinin ana eksen olduğunu ayırt edemezdi.</para><para>Boş slug varsayılanı bilerektir: alan hiç gelmezse değer null değil boş dizedir ve o etiket kendi sayfasına bağlanmaz. Kimliği olmayan bir adres üretmektense bağlantı hiç verilmez.</para></summary>
public class TagViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string Slug { get; set; } = string.Empty;

    /// <summary>Etikete bağlı yayındaki yazı sayısı. Yalnızca bulut okumasında dolar; yazı sayfasındaki çiplerde sıfır kalır.</summary>
    public int PostCount { get; set; }
}
