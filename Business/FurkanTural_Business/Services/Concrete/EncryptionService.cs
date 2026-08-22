using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// AES-CBC; anahtar ve IV yapılandırmadan okunur, iki ayrı bölüm adı (<c>EncryptionSettings</c> ve
/// eski <c>EncryptionConfiguration</c>) sırayla denenir. İkisi de yoksa koda gömülü değerlere düşülür
/// — yani eksik yapılandırma hata vermez, depoda açıkça duran bir anahtarla şifreleme yapılır.
///
/// IV sabit olduğundan aynı düz metin her zaman aynı şifreli metni üretir; eşit değerler şifreli
/// hâllerine bakılarak eşleştirilebilir. Bu, mesaj içeriği için ayrı bir servisin
/// (<see cref="IMessageProtector"/>) bulunma sebebidir.
/// </summary>
public partial class EncryptionService(IConfiguration configuration) : IEncryptionService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        configuration["EncryptionSettings:Key"]
        ?? configuration["EncryptionConfiguration:Key"]
        ?? "IJ%p*85DZ853*96@#@o32ivpR*2o#$@%");

    private readonly byte[] _iv = Encoding.UTF8.GetBytes(
        configuration["EncryptionSettings:IV"]
        ?? configuration["EncryptionConfiguration:IV"]
        ?? "31eH*208Z%#W2**E");

    public Result<string> Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Result<string>.Fail("Lütfen şifrelenecek bir metin girin.", "Input value was empty.");

        string salt1;
        string salt2;
        string textToEncrypt;

        var match = MyRegex().Match(value);

        if (match.Success)
        {
            salt1 = match.Groups[1].Value;
            textToEncrypt = match.Groups[2].Value;
            salt2 = match.Groups[3].Value;
        }
        else
        {
            var random = new Random();
            salt1 = random.Next(1000, 10000).ToString();
            salt2 = random.Next(1000, 10000).ToString();
            textToEncrypt = value;
        }

        var saltedText = $"{salt1}{textToEncrypt}{salt2}";
        var encrypted = EncryptString(saltedText);

        return Result<string>.Ok($"{salt1}:{encrypted}:{salt2}");
    }

    public Result<string> Decrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Result<string>.Fail("Lütfen çözülecek bir şifre girin.", "Input value was empty.");

        var match = MyEncryptedRegex().Match(value);
        if (!match.Success)
            return Result<string>.Fail("Üzgünüz, şifreyi çözemedik. Biz şifrelememiş olabilir miyiz?", "Invalid format. Expected regex match failed.");

        string salt1 = match.Groups[1].Value;
        string encryptedText = match.Groups[2].Value;
        string salt2 = match.Groups[3].Value;

        try
        {
            var decryptedFull = DecryptString(encryptedText);

            if (!decryptedFull.StartsWith(salt1) || !decryptedFull.EndsWith(salt2))
                return Result<string>.Fail("Şifre çözüldü fakat veri bütünlüğü bozuk görünüyor.", "Salt mismatch. Decrypted content did not match expected salts.");

            var originalText = decryptedFull.Substring(salt1.Length, decryptedFull.Length - salt1.Length - salt2.Length);
            return Result<string>.Ok(originalText);
        }
        catch (CryptographicException)
        {
            return Result<string>.Fail("Şifreleme anahtarı geçersiz veya veri bozuk.", "CryptographicException: Decryption failed.");
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Bir hata oluştu.", $"Exception: {ex.Message}");
        }
    }

    private string EncryptString(string plainText)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        swEncrypt.Write(plainText);
        swEncrypt.Close();

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private string DecryptString(string cipherText)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;

        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    [GeneratedRegex("^(\\d{4}):(.*):(\\d{4})$")]
    private static partial Regex MyRegex();

    [GeneratedRegex("^(\\d{4}):(.*):(\\d{4})$")]
    private static partial Regex MyEncryptedRegex();
}