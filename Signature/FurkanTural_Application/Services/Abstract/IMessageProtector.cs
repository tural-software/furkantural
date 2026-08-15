namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Sohbet mesajı içeriğinin veri tabanında şifreli tutulması. Saklanan değer kendini ön ekiyle
/// tanıtır; IsProtected bu ön eke bakar ve şifreleme devreye girmeden önce yazılmış eski kayıtları
/// ayırt etmeye yarar. Protect ile Unprotect null ve boş değeri olduğu gibi geçirir, bu yüzden metin
/// taşımayan mesajlarda ayrıca kontrol gerekmez. Aynı düz metin her seferinde farklı şifreli metin
/// üretir, dolayısıyla eşit içerikler şifreli hâllerine bakılarak eşleştirilemez.
/// </summary>
public interface IMessageProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? stored);
    bool IsProtected(string? stored);
}