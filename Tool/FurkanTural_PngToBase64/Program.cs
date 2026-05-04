using System.Text.Json;

Console.WriteLine("PNG/JPG/WebP/GIF → Base64 Dönüştürücüye Hoş Geldiniz!");

string[] destekliUzantilar = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

string? docsKlasor = null;

string? aranacakDizin = AppContext.BaseDirectory;
while (aranacakDizin is not null)
{
    string aday = Path.Combine(aranacakDizin, "docs");
    if (Directory.Exists(aday))
    {
        docsKlasor = aday;
        break;
    }
    aranacakDizin = Directory.GetParent(aranacakDizin)?.FullName;
}

if (docsKlasor is null)
{
    string? calismaDir = Directory.GetCurrentDirectory();
    while (calismaDir is not null)
    {
        string aday = Path.Combine(calismaDir, "docs");
        if (Directory.Exists(aday))
        {
            docsKlasor = aday;
            break;
        }
        calismaDir = Directory.GetParent(calismaDir)?.FullName;
    }
}

if (docsKlasor is null)
{
    Console.WriteLine("HATA: 'docs' klasörü bulunamadı.");
    return;
}

Console.WriteLine("Dosyalar okunuyor...");

string[] dosyalar = Directory.GetFiles(docsKlasor)
    .Where(f => destekliUzantilar.Contains(Path.GetExtension(f).ToLowerInvariant()))
    .ToArray();

Console.WriteLine($"Bulunan dosya sayısı: {dosyalar.Length}");

if (dosyalar.Length == 0)
{
    Console.WriteLine("İşlenecek görsel dosyası bulunamadı.");
    return;
}

Console.WriteLine("İşleme başlanıyor...");

var base64Sozluk = new Dictionary<string, string>();

foreach (string dosyaYolu in dosyalar)
{
    byte[] dosyaBytes = await File.ReadAllBytesAsync(dosyaYolu);
    string base64 = Convert.ToBase64String(dosyaBytes);
    base64Sozluk[Path.GetFileName(dosyaYolu)] = base64;
}

Console.WriteLine("İşlem tamamlandı.");

var jsonCikti = new { Images = base64Sozluk };
var jsonMetin = JsonSerializer.Serialize(jsonCikti, new JsonSerializerOptions { WriteIndented = true });

string responseYolu = Path.Combine(Directory.GetParent(docsKlasor)!.FullName, "response.json");
await File.WriteAllTextAsync(responseYolu, jsonMetin);

Console.WriteLine($"Sonuçlar '{responseYolu}' dosyasına yazıldı.");
