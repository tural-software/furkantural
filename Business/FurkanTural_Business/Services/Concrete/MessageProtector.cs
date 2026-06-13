using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// AES-256-GCM ile mesaj içeriği şifreleyici (kimlik doğrulamalı; kurcalama tag ile yakalanır).
/// Saklama biçimi kendini tanımlar: <c>ENC1:&lt;base64(nonce[12] | ciphertext | tag[16])&gt;</c>.
/// Mesaj başına <b>rastgele nonce</b> üretilir — sabit IV'li EncryptionService'ten farklı olarak
/// aynı düz metin her seferinde farklı şifreli metne dönüşür (kalıp/eşitlik sızıntısı olmaz).
/// </summary>
public sealed class MessageProtector : IMessageProtector
{
    private const string Prefix = "ENC1:";
    private const int NonceSize = 12;  // AES-GCM standart nonce
    private const int TagSize = 16;    // AES-GCM tam tag

    private readonly byte[] _key;

    public MessageProtector(IConfiguration configuration)
    {
        var configured = configuration["ChatEncryption:Key"];

        // Fail-fast: anahtar yoksa veya hâlâ git'teki public placeholder ise BAŞLATMA.
        // ("CHANGE_ME..." ya da "####:base64:####" konvansiyonu.) Aksi halde herkese açık bir
        // string'den türetilmiş zayıf anahtarla şifreleme yapılır ki bu hiç şifrelememekten beterdir.
        if (string.IsNullOrWhiteSpace(configured)
            || configured.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || configured.Contains("####"))
            throw new InvalidOperationException(
                "ChatEncryption:Key yapılandırılmamış (placeholder). Mesaj at-rest şifrelemesi için gerçek bir gizli anahtar gerekir.");

        // Anahtar herhangi bir uzunlukta string olabilir → SHA-256 ile sabit 32 baytlık AES-256 anahtarına türet.
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

        // nonce | ciphertext | tag → tek base64 bloğu
        var packed = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, packed, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, packed, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, packed, NonceSize + cipherBytes.Length, TagSize);

        return Prefix + Convert.ToBase64String(packed);
    }

    public string? Unprotect(string? stored)
    {
        if (!IsProtected(stored))
            return stored; // legacy düz metin → olduğu gibi

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
            // Önek tesadüfen eşleşmiş gerçek bir düz metin (örn. kullanıcı "ENC1:..." yazdı) ya da
            // anahtar değişmiş olabilir. GCM tag doğrulaması başarısızsa metni ham döndür (veri kaybı yok).
            return stored;
        }
    }

    public bool IsProtected(string? stored)
        => stored is not null && stored.StartsWith(Prefix, StringComparison.Ordinal);
}
