namespace FurkanTural_Application.Services.Abstract;

/// <summary>Sohbet mesajı içeriğinin veri tabanında şifreli tutulması. Saklanan değer kendini ön ekiyle tanıtır; IsProtected bu ön eke bakar ve şifreleme devreye girmeden önce yazılmış eski kayıtları ayırt etmeye yarar. Protect ile Unprotect null ve boş değeri olduğu gibi geçirir, bu yüzden metin taşımayan mesajlarda ayrıca kontrol gerekmez. Aynı düz metin her seferinde farklı şifreli metin üretir, dolayısıyla eşit içerikler şifreli hâllerine bakılarak eşleştirilemez.<para>Taraf kimlikleri şifrelemeye girdi olarak verilir ve şifreli metni o konuşmaya bağlar: başka bir konuşmanın satırına taşınan değer artık çözülemez. Sıra önemsizdir, gönderen ile alıcı yer değiştirse de aynı bağ kurulur — konuşma listesi son mesajı okurken hangi tarafın yazdığını bilmez, yalnızca konuşmanın iki ucunu bilir.</para><para>Çözme başarısız olduğunda çağırana şifreli metin değil, okunamadığını söyleyen sabit bir metin döner; sessizce ham değeri geri vermek, anahtarın değiştiğini kimsenin fark etmemesi demekti.</para></summary>
public interface IMessageProtector
{
    string? Protect(string? plaintext, int userAId, int userBId);
    string? Unprotect(string? stored, int userAId, int userBId);
    bool IsProtected(string? stored);
}
