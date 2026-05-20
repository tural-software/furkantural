using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

// ── Argüman ayrıştırma ────────────────────────────────────────────
// Kullanım:
//   FurkanTural_Crypt encrypt "düz metin"          → Production appsettings
//   FurkanTural_Crypt decrypt "5497:base64:4559"   → Production appsettings
//   FurkanTural_Crypt encrypt "düz metin" --dev    → Development appsettings
//   (argümansız)                                   → Etkileşimli mod

bool isDev  = args.Contains("--dev", StringComparer.OrdinalIgnoreCase);
string? command    = null;
string? inputValue = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] is "encrypt" or "decrypt" && command is null)
    {
        command = args[i];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            inputValue = args[++i];
    }
}

// ── appsettings dosyasını bul ─────────────────────────────────────
var appsettingsPath = FindAppsettingsPath(isDev);
if (appsettingsPath is null)
{
    PrintError("API appsettings dosyası bulunamadı. Çözüm kökü altında 'Web/FurkanTural_API/' aranıyor.");
    return 1;
}

var (encKey, encIv) = ReadEncryptionKeys(appsettingsPath);
var envLabel = isDev ? "Development" : "Production";

PrintDim($"[{envLabel}] {appsettingsPath}");

// ── Doğrudan komut modu ───────────────────────────────────────────
if (command is not null)
{
    if (string.IsNullOrEmpty(inputValue))
    {
        PrintError("Değer belirtilmedi.");
        return 1;
    }
    ExecuteCommand(command, inputValue, encKey, encIv);
    return 0;
}

// ── Etkileşimli mod ───────────────────────────────────────────────
PrintDim("─────────────────────────────────────────");
PrintDim($"Ortam  : {envLabel}");
PrintDim("Çıkmak için 'quit' yazın.");
PrintDim("─────────────────────────────────────────");

while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("İşlem (encrypt / decrypt / quit): ");
    Console.ResetColor();
    var op = Console.ReadLine()?.Trim().ToLowerInvariant();

    if (op is "q" or "quit" or "exit" or null) break;

    if (op is not ("encrypt" or "decrypt"))
    {
        PrintWarning("Geçersiz komut. 'encrypt', 'decrypt' veya 'quit' girin.");
        continue;
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Değer: ");
    Console.ResetColor();
    var val = Console.ReadLine() ?? "";

    ExecuteCommand(op, val, encKey, encIv);
}

return 0;

// ── Komut çalıştırıcı ─────────────────────────────────────────────
static void ExecuteCommand(string command, string value, string key, string iv)
{
    try
    {
        if (command == "encrypt")
        {
            var result = Encrypt(value, key, iv);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Şifreli  : {result}");
            Console.ResetColor();
        }
        else
        {
            var result = Decrypt(value, key, iv);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Çözüldü  : {result}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        PrintError(ex.Message);
    }
}

// ── Şifreleme ─────────────────────────────────────────────────────
static string Encrypt(string plainText, string keyStr, string ivStr)
{
    var salt1  = Random.Shared.Next(1000, 10000).ToString();
    var salt2  = Random.Shared.Next(1000, 10000).ToString();
    var salted = $"{salt1}{plainText}{salt2}";

    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes(keyStr);
    aes.IV  = Encoding.UTF8.GetBytes(ivStr);

    var msEncrypt = new MemoryStream();
    using (var csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
    using (var swEncrypt = new StreamWriter(csEncrypt))
        swEncrypt.Write(salted);

    return $"{salt1}:{Convert.ToBase64String(msEncrypt.ToArray())}:{salt2}";
}

// ── Çözme ─────────────────────────────────────────────────────────
static string Decrypt(string cipher, string keyStr, string ivStr)
{
    var m = Regex.Match(cipher, @"^(\d{4}):(.+):(\d{4})$");
    if (!m.Success)
        throw new ArgumentException("Geçersiz format. Beklenen: 4rakam:base64:4rakam");

    var salt1 = m.Groups[1].Value;
    var b64   = m.Groups[2].Value;
    var salt2 = m.Groups[3].Value;

    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes(keyStr);
    aes.IV  = Encoding.UTF8.GetBytes(ivStr);

    using var ms = new MemoryStream(Convert.FromBase64String(b64));
    using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);
    var decrypted = sr.ReadToEnd();

    if (!decrypted.StartsWith(salt1, StringComparison.Ordinal) ||
        !decrypted.EndsWith(salt2, StringComparison.Ordinal))
        throw new InvalidOperationException("Salt doğrulaması başarısız. Anahtar yanlış veya veri bozuk.");

    return decrypted[salt1.Length..^salt2.Length];
}

// ── Appsettings okuma ─────────────────────────────────────────────
static (string Key, string IV) ReadEncryptionKeys(string path)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var section = doc.RootElement.GetProperty("EncryptionSettings");
    return (
        section.GetProperty("Key").GetString() ?? throw new InvalidOperationException("EncryptionSettings:Key bulunamadı."),
        section.GetProperty("IV").GetString()  ?? throw new InvalidOperationException("EncryptionSettings:IV bulunamadı.")
    );
}

// ── appsettings.json yolu arama ───────────────────────────────────
// Binary hangi dizinde çalışırsa çalışsın, üst dizinleri tarayarak
// çözüm kökündeki Web/FurkanTural_API/ klasörünü bulur.
static string? FindAppsettingsPath(bool isDev)
{
    var fileName = isDev ? "appsettings.Development.json" : "appsettings.json";
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "Web", "FurkanTural_API", fileName);
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

// ── Konsol yardımcıları ───────────────────────────────────────────
static void PrintError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Hata: {msg}");
    Console.ResetColor();
}

static void PrintWarning(string msg)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(msg);
    Console.ResetColor();
}

static void PrintDim(string msg)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(msg);
    Console.ResetColor();
}

