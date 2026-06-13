namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Sohbet mesajı içeriğini <b>at-rest</b> (veritabanında) şifreler. Amaç: veritabanı/yedek
/// hırsızlığı durumunda içeriğin anahtarsız okunamaması. Bu uçtan uca şifreleme DEĞİLDİR —
/// sunucu anahtarı elinde tutar, kullanıcıya/yetkiliye okutmak için çözer (KVKK sözleşmesiyle uyumlu).
/// Geri çözülebilir olmalıdır; bu yüzden parola gibi tek yönlü hash kullanılmaz — bkz. IPasswordHasher.
/// </summary>
public interface IMessageProtector
{
    /// <summary>Düz metni şifreler ("ENC1:" önekli, kendini tanımlayan format). null/boş ise olduğu gibi döner.</summary>
    string? Protect(string? plaintext);

    /// <summary>Şifreliyse çözer; şifreli değilse (legacy düz metin) olduğu gibi döner.</summary>
    string? Unprotect(string? stored);

    /// <summary>Saklanan değer bu şemayla şifrelenmiş mi? (Legacy düz metin kayıtları ayırt etmek için.)</summary>
    bool IsProtected(string? stored);
}
