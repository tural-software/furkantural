using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>AES-256-GCM; saklanan değer <c>ENC1:</c> önekinin ardından tek base64 blokta nonce, şifreli metin ve doğrulama etiketini taşır. Anahtar <c>ChatEncryption:Key</c>'ten okunur, herhangi bir uzunlukta olabilir ve SHA-256 ile 32 bayta indirgenir.<para>Yapıcı, anahtar yoksa veya hâlâ depodaki yer tutucuysa istisna fırlatır: herkesin görebildiği bir dizeden türetilmiş anahtarla şifrelemek, hiç şifrelememekten daha yanıltıcıdır.</para><para>Çözme başarısız olursa saklanan değer olduğu gibi geri döner, istisna fırlamaz. Bu, anahtar değişmişse veya kullanıcı gerçekten <c>ENC1:</c> ile başlayan bir metin yazmışsa veri kaybını önler; karşılığında bozuk çözme ile düz metin çağıran tarafından ayırt edilemez.</para></summary>
public sealed class MessageProtector : IMessageProtector
{
    private const string Prefix = "ENC1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public MessageProtector(IConfiguration configuration)
    {
        var configured = configuration["ChatEncryption:Key"];

        if (string.IsNullOrWhiteSpace(configured)
            || configured.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || configured.Contains("####"))
            throw new InvalidOperationException(
                "ChatEncryption:Key yapılandırılmamış (placeholder). Mesaj at-rest şifrelemesi için gerçek bir gizli anahtar gerekir.");

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
    }

    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var packed = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, packed, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, packed, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, packed, NonceSize + cipherBytes.Length, TagSize);

        return Prefix + Convert.ToBase64String(packed);
    }

    public string? Unprotect(string? stored)
    {
        if (!IsProtected(stored))
            return stored;

        try
        {
            var packed = Convert.FromBase64String(stored![Prefix.Length..]);
            if (packed.Length < NonceSize + TagSize)
                return stored;

            var nonce = packed[..NonceSize];
            var tag = packed[^TagSize..];
            var cipherBytes = packed[NonceSize..^TagSize];
            var plainBytes = new byte[cipherBytes.Length];

            using (var aes = new AesGcm(_key, TagSize))
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return stored;
        }
    }

    public bool IsProtected(string? stored)
        => stored is not null && stored.StartsWith(Prefix, StringComparison.Ordinal);
}
