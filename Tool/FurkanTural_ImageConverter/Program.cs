using System.Text;
using ImageMagick;

// FurkanTural_ImageConverter — PNG görselleri web (WebP) ve ICO formatlarına dönüştürür.
// net10.0 konsol aracı (ImageMagick/Magick.NET tabanlı).
//
// Kullanım:
//   FurkanTural_ImageConverter [girdi] [seçenekler]
//     girdi : bir .png dosyası VEYA png içeren bir klasör (verilmezse: çalışma dizini)
//
//   Seçenekler:
//     --webp              Yalnız WebP üret
//     --ico               Yalnız ICO üret
//                         (ikisi de verilmezse HER İKİSİ üretilir)
//     -o, --out <dizin>   Çıktı dizini (varsayılan: kaynağın yanı)
//     -q, --quality <n>   WebP kalitesi 1-100 (varsayılan 90)
//         --lossless      Kayıpsız WebP
//     -s, --sizes a,b,c   ICO kareleri (varsayılan 16,32,48,64,128,256)
//     -r, --recurse       Klasör girdisinde alt klasörleri de tara
//     -h, --help          Bu yardımı göster

return Run(args);

static int Run(string[] args)
{
    // Türkçe karakter + işaretler düzgün görünsün diye konsolu UTF-8'e al.
    try { Console.OutputEncoding = Encoding.UTF8; } catch { /* yönlendirilmiş çıktıda önemsiz */ }

    if (args.Length == 0 || args.Any(a => a is "-h" or "--help" or "/?"))
    {
        YardimYaz();
        return 0;
    }

    // ── Argümanları ayrıştır ────────────────────────────────────────────────
    string? girdi = null;
    string? cikisDizin = null;
    bool webp = false, ico = false, lossless = false, recurse = false;
    uint kalite = 90;
    int[] icoKareleri = [16, 32, 48, 64, 128, 256];

    for (int i = 0; i < args.Length; i++)
    {
        string a = args[i];
        switch (a)
        {
            case "--webp": webp = true; break;
            case "--ico": ico = true; break;
            case "--lossless": lossless = true; break;
            case "-r" or "--recurse": recurse = true; break;

            case "-o" or "--out":
                if (Deger(args, ref i, a) is not { } o) return 2;
                cikisDizin = o;
                break;

            case "-q" or "--quality":
                if (Deger(args, ref i, a) is not { } q) return 2;
                if (!uint.TryParse(q, out kalite) || kalite < 1 || kalite > 100)
                    return Hata($"Geçersiz kalite: '{q}' (1-100 arası bir sayı olmalı).");
                break;

            case "-s" or "--sizes":
                if (Deger(args, ref i, a) is not { } s) return 2;
                if (KareleriAyikla(s) is not { } kr) return 2;
                icoKareleri = kr;
                break;

            default:
                if (a.StartsWith('-'))
                    return Hata($"Bilinmeyen seçenek: '{a}'. Yardım için --help.");
                if (girdi is not null)
                    return Hata($"Birden çok girdi verildi ('{girdi}' ve '{a}'). Tek dosya/klasör verin.");
                girdi = a;
                break;
        }
    }

    // Format seçilmemişse ikisini de üret.
    if (!webp && !ico) { webp = true; ico = true; }

    // ── Girdi PNG dosyalarını topla ─────────────────────────────────────────
    girdi ??= Directory.GetCurrentDirectory();

    List<string> pngler = [];
    if (File.Exists(girdi))
    {
        if (!PngMi(girdi)) return Hata($"Girdi bir PNG dosyası değil: '{girdi}'.");
        pngler.Add(Path.GetFullPath(girdi));
    }
    else if (Directory.Exists(girdi))
    {
        var arama = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        pngler.AddRange(Directory.EnumerateFiles(girdi, "*.png", arama)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Select(Path.GetFullPath));
    }
    else
    {
        return Hata($"Girdi bulunamadı: '{girdi}'.");
    }

    if (pngler.Count == 0)
    {
        Console.WriteLine("İşlenecek PNG dosyası bulunamadı.");
        return 0;
    }

    string hedefler = string.Join(" + ", new[] { webp ? "WebP" : null, ico ? "ICO" : null }.Where(x => x is not null));
    Console.WriteLine("FurkanTural_ImageConverter — PNG → WebP/ICO");
    Console.WriteLine($"Bulunan PNG: {pngler.Count} · Hedef: {hedefler}");
    if (cikisDizin is not null)
    {
        Directory.CreateDirectory(cikisDizin);
        Console.WriteLine($"Çıktı dizini: {Path.GetFullPath(cikisDizin)}");
    }
    Console.WriteLine();

    // ── Dönüştür ────────────────────────────────────────────────────────────
    int basarili = 0, hatali = 0;
    foreach (string png in pngler)
    {
        string ad = Path.GetFileNameWithoutExtension(png);
        string hedefDizin = cikisDizin ?? Path.GetDirectoryName(png)!;
        try
        {
            if (webp)
            {
                string cikis = Path.Combine(hedefDizin, ad + ".webp");
                WebpYaz(png, cikis, kalite, lossless);
                Console.WriteLine($"  ✓ {Path.GetFileName(png)} → {Path.GetFileName(cikis)}");
            }

            if (ico)
            {
                string cikis = Path.Combine(hedefDizin, ad + ".ico");
                IcoYaz(png, cikis, icoKareleri);
                Console.WriteLine($"  ✓ {Path.GetFileName(png)} → {Path.GetFileName(cikis)} ({string.Join("/", icoKareleri)})");
            }

            basarili++;
        }
        catch (Exception ex)
        {
            hatali++;
            Hata($"{Path.GetFileName(png)}: {ex.Message}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Tamamlandı. Başarılı: {basarili}, Hatalı: {hatali}.");
    return hatali == 0 ? 0 : 1;
}

// PNG'yi WebP olarak yazar. lossless=true ise kayıpsız, değilse verilen kalitede kayıplı.
static void WebpYaz(string kaynak, string cikis, uint kalite, bool lossless)
{
    using var image = new MagickImage(kaynak);
    image.Quality = kalite;
    if (lossless)
        image.Settings.SetDefine(MagickFormat.WebP, "lossless", true);
    image.Format = MagickFormat.WebP;
    image.Write(cikis);
}

// PNG'yi çok-çözünürlüklü ICO olarak yazar (her kare şeffaf zeminde tam kare).
static void IcoYaz(string kaynak, string cikis, int[] kareler)
{
    using var koleksiyon = new MagickImageCollection();
    foreach (int boyut in kareler)
    {
        var kare = new MagickImage(kaynak);
        kare.BackgroundColor = MagickColors.Transparent;
        // En-boy oranını koruyarak boyuta sığdır, sonra şeffaf zeminle tam kareye oturt.
        kare.Resize(new MagickGeometry((uint)boyut, (uint)boyut));
        kare.Extent(new MagickGeometry((uint)boyut, (uint)boyut), Gravity.Center, MagickColors.Transparent);
        koleksiyon.Add(kare);
    }
    koleksiyon.Write(cikis, MagickFormat.Ico);
}

// "--out value" gibi bir seçeneğin değerini güvenle okur; eksikse hata basıp null döner.
static string? Deger(string[] args, ref int i, string secenek)
{
    if (i + 1 >= args.Length)
    {
        Hata($"'{secenek}' için bir değer bekleniyordu.");
        return null;
    }
    return args[++i];
}

// "16,32,48" → int[]; geçersizse hata basıp null döner.
static int[]? KareleriAyikla(string ham)
{
    var liste = new List<int>();
    foreach (string parca in ham.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!int.TryParse(parca, out int boyut) || boyut < 1 || boyut > 1024)
        {
            Hata($"Geçersiz ICO karesi: '{parca}' (1-1024 arası olmalı).");
            return null;
        }
        if (!liste.Contains(boyut)) liste.Add(boyut);
    }
    if (liste.Count == 0)
    {
        Hata("En az bir geçerli ICO karesi verilmeli.");
        return null;
    }
    liste.Sort();
    return [.. liste];
}

static bool PngMi(string yol) =>
    string.Equals(Path.GetExtension(yol), ".png", StringComparison.OrdinalIgnoreCase);

// Kırmızı "HATA: ..." satırını stderr'e basar ve daima 2 (kullanım hatası) döner —
// böylece çağıran yerde `return Hata(...)` deyip hem mesaj hem çıkış kodu verir.
static int Hata(string mesaj)
{
    ConsoleColor eski = Console.ForegroundColor;
    try { Console.ForegroundColor = ConsoleColor.Red; } catch { }
    Console.Error.WriteLine("HATA: " + mesaj);
    try { Console.ForegroundColor = eski; } catch { }
    return 2;
}

static void YardimYaz()
{
    Console.WriteLine(
        """
        FurkanTural_ImageConverter — PNG görselleri web (WebP) ve ICO formatına dönüştürür.

        Kullanım:
          FurkanTural_ImageConverter [girdi] [seçenekler]

          girdi                 Bir .png dosyası veya png içeren bir klasör.
                                Verilmezse çalışma dizini taranır.

        Seçenekler:
          --webp                Yalnız WebP üret
          --ico                 Yalnız ICO üret
                                (ikisi de verilmezse her ikisi üretilir)
          -o, --out <dizin>     Çıktı dizini (varsayılan: kaynağın yanı)
          -q, --quality <n>     WebP kalitesi 1-100 (varsayılan 90)
              --lossless        Kayıpsız WebP
          -s, --sizes a,b,c     ICO kareleri (varsayılan 16,32,48,64,128,256)
          -r, --recurse         Klasör girdisinde alt klasörleri de tara
          -h, --help            Bu yardımı göster

        Örnekler:
          FurkanTural_ImageConverter logo.png
          FurkanTural_ImageConverter .\images --webp -q 80
          FurkanTural_ImageConverter favicon.png --ico -s 16,32,48,256
          FurkanTural_ImageConverter .\assets -r -o .\dist
        """);
}
