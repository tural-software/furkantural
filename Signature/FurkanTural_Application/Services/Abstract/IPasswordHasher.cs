namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Tek yönlü parola özetleme (hash). Parolalar geri çözülemez biçimde saklanır;
/// eski (geri çözülebilir AES) kayıtlar login sırasında şeffaf olarak bu formata taşınır.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Parolayı tuzlu PBKDF2 özetine çevirir (kendi kendini tanımlayan format).</summary>
    string Hash(string password);

    /// <summary>Parolayı saklanan özetle sabit-zamanlı karşılaştırır.</summary>
    bool Verify(string password, string stored);

    /// <summary>Saklanan değer bu hasher'ın formatında mı? (Legacy AES kayıtlarını ayırt eder.)</summary>
    bool IsHashed(string? stored);
}
