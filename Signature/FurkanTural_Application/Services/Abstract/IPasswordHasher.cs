namespace FurkanTural_Application.Services.Abstract;

/// <summary>Parola özetleme. Saklanan değer kullanılan algoritmayı ve tuzu kendi içinde taşır, bu yüzden Verify'a bunlar ayrıca verilmez ve iterasyon sayısı ileride artsa bile eski kayıtlar doğrulanmaya devam eder. IsHashed eski kayıtlar içindir: bu şemayla üretilmemiş parolalar hâlâ veri tabanında bulunabilir, onlar geri çözülebilir yolla doğrulanıp doğru girildiğinde buraya taşınır (bkz. <see cref="IEncryptionService"/>).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string stored);
    bool IsHashed(string? stored);
}
