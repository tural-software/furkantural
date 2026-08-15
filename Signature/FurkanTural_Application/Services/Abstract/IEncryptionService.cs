using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Yapılandırma değerleri için geri çözülebilir şifreleme. Çıktı biçimi <c>0000:base64:0000</c>'dır;
/// baştaki ve sondaki dört hane şifrelenen metnin içine de karıştığı için bütünlük işareti görevi
/// görür ve bu desene uymayan girdi çözülemez. Encrypt'e zaten bu desende bir değer verilirse mevcut
/// haneler korunur, yeniden üretilmez. Parola saklamak için kullanılmaz — geri çözülebilir olduğundan
/// oraya <see cref="IPasswordHasher"/> girer.
/// </summary>
public interface IEncryptionService
{
    Result<string> Encrypt(string value);
    Result<string> Decrypt(string value);
}