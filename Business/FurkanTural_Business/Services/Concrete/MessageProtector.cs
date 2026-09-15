using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>AES-256-GCM. Saklanan değer <c>ENC2:</c> önekinin ardından anahtar kimliğini ve tek base64 blokta nonce, şifreli metin ile doğrulama etiketini taşır. Anahtar <c>ChatEncryption:Key</c>'ten okunur, herhangi bir uzunlukta olabilir ve SHA-256 ile 32 bayta indirgenir.<para>Yapıcı, anahtar yoksa veya hâlâ depodaki yer tutucuysa istisna fırlatır: herkesin görebildiği bir dizeden türetilmiş anahtarla şifrelemek, hiç şifrelememekten daha yanıltıcıdır.</para><para>Konuşmanın iki ucu ilişkili veri (associated data) olarak şifrelemeye girer. Şifreli metin böylece o konuşmaya bağlanır: veri tabanına yazabilen biri bir satırın içeriğini başka bir konuşmanın satırına kopyalarsa değer artık çözülmez. Kimlikler küçükten büyüğe sıralanır, çünkü konuşma listesi son mesajı okurken yalnızca konuşmanın iki ucunu bilir, hangi tarafın yazdığını bilmez.</para><para>Anahtar kimliği anahtarın kendisinden değil ayrı bir türetmeden gelir; saklanan değerde durduğu için anahtarın baytlarını ele veren bir parça olmamalıdır. Kimliği tutmayan bir kayıt çözülmeye hiç kalkışılmadan okunamaz sayılır — anahtar döndürüldüğünde hangi satırların geride kaldığı ancak böyle görünür.</para><para>Çözülemeyen değer için sabit bir metin döner. Tek istisna eski <c>ENC1:</c> biçimidir: o dönemin kayıtları ilişkili veri taşımaz ve olduğu gibi çözülür; yapısı hiç tanınmayan bir <c>ENC1:</c> metni ise kullanıcının kendi yazdığı metin olabileceği için değiştirilmeden geri verilir.</para></summary>
public sealed class MessageProtector : IMessageProtector
{
    private const string Prefix = "ENC2:";
    private const string LegacyPrefix = "ENC1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeyIdLength = 8;

    public const string UnreadableContent = "(bu mesaj çözülemedi)";

    private readonly byte[] _key;
    private readonly string _keyId;

    public MessageProtector(IConfiguration configuration)
    {
        var configured = configuration["ChatEncryption:Key"];

        if (string.IsNullOrWhiteSpace(configured)
            || configured.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || configured.Contains("####"))
            throw new InvalidOperationException(
                "ChatEncryption:Key yapılandırılmamış (placeholder). Mesaj at-rest şifrelemesi için gerçek bir gizli anahtar gerekir.");

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        _keyId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("kid|" + configured)))[..KeyIdLength]
            .ToLowerInvariant();
    }

    public string? Protect(string? plaintext, int userAId, int userBId)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag, Conversation(userAId, userBId));

        var packed = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, packed, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, packed, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, packed, NonceSize + cipherBytes.Length, TagSize);

        return $"{Prefix}{_keyId}:{Convert.ToBase64String(packed)}";
    }

    public string? Unprotect(string? stored, int userAId, int userBId)
    {
        if (stored is null)
            return stored;

        if (stored.StartsWith(Prefix, StringComparison.Ordinal))
            return Open(stored[Prefix.Length..], Conversation(userAId, userBId));

        if (stored.StartsWith(LegacyPrefix, StringComparison.Ordinal))
            return OpenLegacy(stored[LegacyPrefix.Length..], stored);

        return stored;
    }

    public bool IsProtected(string? stored)
        => stored is not null
        && (stored.StartsWith(Prefix, StringComparison.Ordinal)
         || stored.StartsWith(LegacyPrefix, StringComparison.Ordinal));

    private string Open(string body, byte[] associatedData)
    {
        var separator = body.IndexOf(':');
        if (separator <= 0)
            return UnreadableContent;

        if (!string.Equals(body[..separator], _keyId, StringComparison.Ordinal))
            return UnreadableContent;

        if (!TryUnpack(body[(separator + 1)..], out var nonce, out var cipherBytes, out var tag))
            return UnreadableContent;

        return Decrypt(nonce, cipherBytes, tag, associatedData) ?? UnreadableContent;
    }

    private string OpenLegacy(string body, string stored)
    {
        if (!TryUnpack(body, out var nonce, out var cipherBytes, out var tag))
            return stored;

        return Decrypt(nonce, cipherBytes, tag, null) ?? UnreadableContent;
    }

    private string? Decrypt(byte[] nonce, byte[] cipherBytes, byte[] tag, byte[]? associatedData)
    {
        try
        {
            var plainBytes = new byte[cipherBytes.Length];

            using (var aes = new AesGcm(_key, TagSize))
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static bool TryUnpack(string body, out byte[] nonce, out byte[] cipherBytes, out byte[] tag)
    {
        nonce = [];
        cipherBytes = [];
        tag = [];

        byte[] packed;
        try
        {
            packed = Convert.FromBase64String(body);
        }
        catch (FormatException)
        {
            return false;
        }

        if (packed.Length < NonceSize + TagSize)
            return false;

        nonce = packed[..NonceSize];
        tag = packed[^TagSize..];
        cipherBytes = packed[NonceSize..^TagSize];
        return true;
    }

    private static byte[] Conversation(int userAId, int userBId)
        => Encoding.UTF8.GetBytes($"conv:{Math.Min(userAId, userBId)}:{Math.Max(userAId, userBId)}");
}
