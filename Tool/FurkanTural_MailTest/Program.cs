using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_MailTest;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = Encoding.UTF8;

if (HasFlag("--help") || HasFlag("-h"))
{
    PrintUsage();
    return 0;
}

var recipient = Option("--to");
var skipDns = HasFlag("--no-dns");
var timeout = TimeSpan.FromSeconds(int.TryParse(Option("--timeout"), out var seconds) && seconds > 0 ? seconds : 30);
var configPath = Option("--config") is { } explicitPath
    ? Path.GetFullPath(explicitPath)
    : FindAppsettingsPath(HasFlag("--dev") ? "appsettings.Development.json" : "appsettings.json");

if (configPath is null || !File.Exists(configPath))
{
    Report.Fail(configPath is null
        ? "API ayar dosyası bulunamadı; çözüm kökü altında Web/FurkanTural_API/ aranıyor. Başka bir dosya için --config verin."
        : $"Ayar dosyası yok: {configPath}");
    return 2;
}

Report.Header(configPath);

if (args.Length == 0 && !Console.IsInputRedirected)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Test postasının gideceği adres (boş bırakılırsa gönderilmez): ");
    Console.ResetColor();
    recipient = Console.ReadLine()?.Trim() is { Length: > 0 } typed ? typed : null;
}

if (recipient is not null && !MailAddress.TryCreate(recipient, out _))
{
    Report.Fail($"Alıcı adresi geçersiz: {recipient}");
    return 2;
}

Report.Section("1. Yapılandırma");

IConfiguration configuration;
EncryptionService encryption;

try
{
    configuration = new ConfigurationBuilder().AddJsonFile(configPath, optional: false).Build();
    encryption = new EncryptionService(configuration);
}
catch (Exception ex)
{
    Report.Fail(ex.Message);
    return Finish(null, sent: false);
}

var settings = SmtpSettings.Resolve(configuration, encryption);
settings.Inspect();

Report.Section("2. DNS");

var host = settings.Host.Value;
IPAddress[] hostAddresses = [];

if (string.IsNullOrWhiteSpace(host))
{
    Report.Info("Sunucu adı boş olduğu için atlandı.");
}
else
{
    try
    {
        using var cancellation = new CancellationTokenSource(timeout);
        var watch = Stopwatch.StartNew();
        hostAddresses = await Dns.GetHostAddressesAsync(host, cancellation.Token);
        Report.Ok($"{host} → {string.Join(", ", hostAddresses.Select(x => x.ToString()))} ({watch.ElapsedMilliseconds} ms)");
    }
    catch (Exception ex)
    {
        Report.Fail($"{host} çözülemedi: {ex.Message}");
    }
}

Report.Section("3. SMTP oturumu");

IPAddress? serverAddress = null;

if (hostAddresses.Length == 0)
    Report.Info("DNS çözülemediği için atlandı.");
else
    serverAddress = await SmtpProbe.RunAsync(settings, recipient, timeout);

Report.Section("4. Gönderim (EmailService)");

var sent = false;

if (recipient is null)
{
    Report.Info("Alıcı verilmediği için posta gönderilmedi. Uçtan uca denemek için: --to adres@alan.com");
}
else
{
    var testId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    var subject = $"Posta testi {testId}";
    using var cancellation = new CancellationTokenSource(timeout);
    var watch = Stopwatch.StartNew();

    try
    {
        await new EmailService(configuration, encryption).SendAsync(recipient, subject, BuildBody(testId, settings), cancellation.Token);
        sent = true;
        Report.Ok($"Sunucu postayı kabul etti ({watch.ElapsedMilliseconds} ms). Konu: \"{subject}\"");
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        Report.Fail($"Gönderim {timeout.TotalSeconds:0} saniye içinde bitmedi.");
    }
    catch (Exception ex)
    {
        var status = ex is SmtpException smtp ? $" (SMTP durum kodu {(int)smtp.StatusCode} {smtp.StatusCode})" : "";
        Report.Fail($"{ex.GetType().Name}: {ex.Message}{status}");

        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
            Report.Info($"↳ {inner.GetType().Name}: {inner.Message}");
    }
}

Report.Section("5. Alan adı (SPF, DMARC)");

if (skipDns)
{
    Report.Info("--no-dns verildiği için atlandı.");
}
else if (!MailAddress.TryCreate(settings.From.Value, out var fromAddress))
{
    Report.Info("Gönderen adresi geçersiz olduğu için atlandı.");
}
else
{
    using var http = new HttpClient();
    await new DomainCheck(http, timeout).RunAsync(fromAddress.Host, serverAddress is null ? hostAddresses : [serverAddress]);
}

return Finish(settings.From.Value, sent);

string? Option(string name)
{
    var index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

bool HasFlag(string name) => args.Contains(name, StringComparer.OrdinalIgnoreCase);

static int Finish(string? from, bool sent)
{
    if (Report.Failures > 0)
        Report.Summary(ConsoleColor.Red, $"Sonuç: {Report.Failures} hata, {Report.Warnings} uyarı. Sorunun kaynağı ilk [HATA] satırıdır.");
    else if (sent)
        Report.Summary(ConsoleColor.Green, $"Sonuç: sunucu postayı kabul etti ({Report.Warnings} uyarı). Gelen kutusunda yoksa sırayla bakın: istenmeyen (spam) klasörü; {from} kutusuna dönen iade postası; alınan postanın Authentication-Results başlığındaki spf, dkim ve dmarc sonuçları.");
    else
        Report.Summary(ConsoleColor.Green, $"Sonuç: bağlantı ve giriş sağlam ({Report.Warnings} uyarı). Uçtan uca denemek için --to adres@alan.com ile yeniden çalıştırın.");

    return Report.Failures > 0 ? 1 : 0;
}

static string BuildBody(string testId, SmtpSettings settings)
{
    var rows = new (string Label, string Value)[]
    {
        ("Test kimliği", testId),
        ("Sunucu", $"{settings.Host.Value}:{settings.PortNumber}"),
        ("Gönderen", settings.From.Value ?? ""),
        ("Makine", Environment.MachineName),
        ("Zaman (UTC)", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
    };

    var cells = string.Concat(rows.Select(x => $"<tr><td><b>{WebUtility.HtmlEncode(x.Label)}</b></td><td>{WebUtility.HtmlEncode(x.Value)}</td></tr>"));
    return $"<p>Bu posta FurkanTural_MailTest aracıyla, sitenin kullandığı EmailService üzerinden gönderildi.</p><table>{cells}</table>";
}

static string? FindAppsettingsPath(string fileName)
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Web", "FurkanTural_API", fileName);
            if (File.Exists(candidate))
                return candidate;
        }
    }

    return null;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Kullanım:
          FurkanTural_MailTest                          API appsettings.json; alıcı adresi sorulur
          FurkanTural_MailTest --dev                    appsettings.Development.json; göndermeden sınar
          FurkanTural_MailTest --config <yol> --to <adres>
                                                        verilen ayar dosyasıyla uçtan uca gönderir

        Seçenekler:
          --to <adres>     Test postası bu adrese EmailService ile gönderilir
          --config <yol>   Web/FurkanTural_API dışındaki bir ayar dosyası
          --dev            appsettings.Development.json kullanılır
          --timeout <sn>   Adım başına süre sınırı (varsayılan 30)
          --no-dns         SPF ve DMARC denetimi atlanır

        Çıkış kodu: 0 hata yok, 1 en az bir hata, 2 geçersiz kullanım.
        """);
}
