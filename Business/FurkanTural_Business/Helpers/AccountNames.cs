using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text;

namespace FurkanTural_Business.Helpers;

/// <summary>Kayıt sırasında kullanıcı adının ve e-postanın tek bir kanonik yazıma indirgenmesi. Tekil indeksler bu değerlere dayandığı için aynı kimliğin iki farklı yazımı iki satır açmamalıdır; ayrıca farklı alfabelerden aynı görünen harflerle başkasının adının taklit edilmesi bu aşamada kesilir.<para>İzin verilen küme İngiliz alfabesi, Türkçe harfler, rakamlar ve üç ayraçtır. Kiril ya da Yunan harfleri görsel olarak aynı görünse de dışarıda kalır: <c>аdmin</c> (Kiril a) ile <c>admin</c> ayrı iki satır olurdu ve okuyan kişi ikisini ayırt edemezdi.</para><para>Ayrılmış adlar karşılaştırılırken ayraçlar atılır ve noktalı/noktasız I ayrımı kaldırılır; aksi hâlde <c>a.d.m.i.n</c> ya da <c>ADMİN</c> listeyi dolaşırdı.</para><para>Buradaki kurallar yalnızca yeni kayıtlara uygulanır. Var olan satırlar olduğu gibi kalır ve sahipleri giriş yapmaya devam eder — geçmişe dönük bir ad değişikliği, kullanıcıyı kendi hesabından eder.</para></summary>
public static class AccountNames
{
    public const int UsernameMinimumLength = 3;

    public const int UsernameMaximumLength = 100;

    public const int EmailMaximumLength = 256;

    public const string TurkishLetters = "çğıöşüÇĞİÖŞÜ";

    public const string Separators = "._-";

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "admin", "administrator", "yonetici", "root", "system", "sistem", "moderator", "moderator",
        "support", "destek", "help", "yardim", "info", "iletisim", "noreply", "postmaster", "webmaster",
        "security", "guvenlik", "furkantural", "chatural", "api", "bot", "official", "resmi",
        "anonymous", "anonim", "deleted", "silinmis", "null", "undefined", "me", "self"
    };

    public static bool TryNormalizeUsername(
        [NotNullWhen(true)] string? username,
        out string normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = string.Empty;

        var trimmed = username?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            error = "Kullanıcı adı boş olamaz.";
            return false;
        }

        trimmed = trimmed.Normalize(NormalizationForm.FormKC);

        if (trimmed.Length < UsernameMinimumLength || trimmed.Length > UsernameMaximumLength)
        {
            error = $"Kullanıcı adı {UsernameMinimumLength}-{UsernameMaximumLength} karakter olmalı.";
            return false;
        }

        if (!trimmed.All(IsAllowedUsernameCharacter))
        {
            error = "Kullanıcı adında yalnızca harf, rakam, nokta, alt çizgi ve tire kullanılabilir.";
            return false;
        }

        if (Reserved.Contains(Fold(trimmed)))
        {
            error = "Bu kullanıcı adı ayrılmış. Lütfen başka bir ad seçin.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }

    public static bool TryNormalizeEmail(
        [NotNullWhen(true)] string? email,
        out string normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = string.Empty;

        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            error = "E-posta boş olamaz.";
            return false;
        }

        if (trimmed.Length > EmailMaximumLength
            || !MailAddress.TryCreate(trimmed, out var parsed)
            || !string.Equals(parsed.Address, trimmed, StringComparison.Ordinal))
        {
            error = "Geçerli bir e-posta adresi girin.";
            return false;
        }

        normalized = trimmed.ToLowerInvariant();
        error = null;
        return true;
    }

    public static bool IsAllowedUsernameCharacter(char character)
        => (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9')
        || Separators.Contains(character)
        || TurkishLetters.Contains(character);

    private static string Fold(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (Separators.Contains(character))
                continue;

            builder.Append(character switch
            {
                'I' or 'İ' or 'ı' => 'i',
                _ => char.ToLowerInvariant(character)
            });
        }

        return builder.ToString();
    }
}
