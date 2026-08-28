using System.Diagnostics.CodeAnalysis;

namespace FurkanTural_Business.Helpers;

public static class PasswordPolicy
{
    public static bool TryValidate(
        [NotNullWhen(true)] string? password,
        [NotNullWhen(false)] out string? error)
    {
        error = Validate(password);
        return error is null;
    }

    public const int MinimumLength = 6;

    public const int GeneratedLength = 12;

    public const string Symbols = "!#$%()*+,-./:;=?@[]^_{|}~";

    public static bool IsAllowed(char character)
        => (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9')
        || Symbols.Contains(character);

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Şifre boş olamaz.";

        if (password.Length < MinimumLength)
            return $"Parola en az {MinimumLength} karakter olmalı.";

        if (password.Any(karakter => !IsAllowed(karakter)))
            return "Parolada kullanılamayan bir karakter var. Yalnızca İngiliz alfabesindeki harfler, "
                 + $"rakamlar ve şu semboller kullanılabilir: {Symbols}";

        var eksik = new List<string>();

        if (!password.Any(char.IsUpper)) eksik.Add("bir büyük harf");
        if (!password.Any(char.IsLower)) eksik.Add("bir küçük harf");
        if (!password.Any(char.IsDigit)) eksik.Add("bir rakam");
        if (!password.Any(Symbols.Contains)) eksik.Add("bir sembol");

        return eksik.Count == 0 ? null : $"Parola en az {Birlestir(eksik)} içermeli.";
    }

    private static string Birlestir(IReadOnlyList<string> parcalar)
        => parcalar.Count == 1
            ? parcalar[0]
            : string.Join(", ", parcalar.Take(parcalar.Count - 1)) + " ve " + parcalar[^1];
}
