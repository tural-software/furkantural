namespace FurkanTural_Business.Helpers;

/// <summary>Slug uzunlukları veri tabanı kolon genişliklerini yansıtır. Hizasız kalırlarsa uzun bir başlık kaydetme anında hata doğurur ve kullanıcının gördüğü şey, adres üretiminin sessizce başarısız olması değil uygulamanın 500'ü olur.</summary>
public static class SlugLimits
{
    public const int Blog = 200;
    public const int Category = 160;

    /// <summary>Çakışma taramasında okunan en fazla satır. Önek eşleşmesi normalde sıfır ya da bir satır döndürür; sınır, elle aynı adres verilmeye çalışılan uç bir durumda sorgunun büyümesini engeller.</summary>
    public const int CollisionScan = 200;
}
