using System.Net.Mail;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_MailTest;

internal enum ValueSource
{
    Plain,
    Decrypted,
    Undecryptable,
    Default,
    Missing
}

internal sealed record SettingValue(string Key, string? Value, ValueSource Source);

internal sealed record SmtpSettings(SettingValue Host, SettingValue Port, SettingValue Username, SettingValue Password, SettingValue From, SettingValue FromName)
{
    public int PortNumber => int.TryParse(Port.Value, out var port) ? port : 587;

    public static SmtpSettings Resolve(IConfiguration configuration, IEncryptionService encryption)
    {
        var username = Read(configuration, encryption, "Username", "messanger@furkantural.com");
        var sender = Read(configuration, encryption, "From", null);
        var fromName = configuration["Smtp:FromName"];

        return new SmtpSettings(
            Read(configuration, encryption, "Host", "smtp.hostinger.com"),
            Read(configuration, encryption, "Port", "587"),
            username,
            Read(configuration, encryption, "Password", null),
            sender.Source == ValueSource.Missing ? sender with { Value = username.Value, Source = ValueSource.Default } : sender,
            fromName is null ? new SettingValue("FromName", "Furkan Tural", ValueSource.Default) : new SettingValue("FromName", fromName, ValueSource.Plain));
    }

    public void Inspect()
    {
        Print("Host", Host, secret: false);
        Print("Port", Port, secret: false);
        Print("Kullanıcı", Username, secret: false);
        Print("Parola", Password, secret: true);
        Print("Gönderen", From, secret: false);
        Print("Gönderen adı", FromName, secret: false);

        var before = Report.Failures;

        foreach (var setting in new[] { Host, Port, Username, Password, From })
        {
            if (setting.Value is not null && setting.Value.StartsWith("CHANGE_ME", StringComparison.Ordinal))
                Report.Fail($"Smtp:{setting.Key} yer tutucu değerde kalmış. Gerçek değerleri taşıyan dosyayı --config ile verin.");
            else if (setting.Value is not null && string.IsNullOrWhiteSpace(setting.Value))
                Report.Fail($"Smtp:{setting.Key} boş.");

            if (setting.Source == ValueSource.Undecryptable)
                Report.Fail($"Smtp:{setting.Key} şifreli görünüyor ama çözülemedi. EmailService bu durumda şifreli metni olduğu gibi kullanır; EncryptionSettings, değeri şifreleyen anahtarla aynı olmayabilir.");
        }

        if (Password.Source == ValueSource.Missing)
            Report.Fail("Smtp:Password yok. EmailService gönderime başlamadan istisna fırlatır.");

        if (Host.Source == ValueSource.Default)
            Report.Warn($"Smtp:Host ayarda yok; EmailService koddaki varsayılana ({Host.Value}) düşer.");

        if (!int.TryParse(Port.Value, out _))
            Report.Warn("Smtp:Port sayı değil; EmailService 587'ye düşer.");
        else if (PortNumber == 465)
            Report.Fail("465 örtük TLS ister ve System.Net.Mail.SmtpClient bunu desteklemez; EmailService bu portla gönderemez. 587 (STARTTLS) kullanın.");

        if (!MailAddress.TryCreate(From.Value, out var sender))
            Report.Fail($"Gönderen adresi geçersiz: {From.Value}");
        else if (MailAddress.TryCreate(Username.Value, out var user) && !string.Equals(sender.Host, user.Host, StringComparison.OrdinalIgnoreCase))
            Report.Warn($"Gönderen ({sender.Host}) ile kullanıcı ({user.Host}) farklı alan adında; sunucu başka bir alan adı adına göndermeyi reddedebilir.");

        if (Report.Failures == before)
            Report.Ok("Ayarlar EmailService'in okuyacağı biçimde çözüldü.");
    }

    private static SettingValue Read(IConfiguration configuration, IEncryptionService encryption, string key, string? fallback)
    {
        var raw = configuration[$"Smtp:{key}"];

        if (raw is null)
            return new SettingValue(key, fallback, fallback is null ? ValueSource.Missing : ValueSource.Default);

        if (string.IsNullOrWhiteSpace(raw) || raw.Split(':').Length != 3)
            return new SettingValue(key, raw, ValueSource.Plain);

        var result = encryption.Decrypt(raw);
        return result.Success
            ? new SettingValue(key, result.Data, ValueSource.Decrypted)
            : new SettingValue(key, raw, ValueSource.Undecryptable);
    }

    private static void Print(string label, SettingValue setting, bool secret)
    {
        var shown = setting.Value is null
            ? "(yok)"
            : secret ? $"ayarlı, {setting.Value.Length} karakter" : setting.Value;

        var origin = setting.Source switch
        {
            ValueSource.Decrypted => "şifreli, çözüldü",
            ValueSource.Undecryptable => "şifreli, çözülemedi",
            ValueSource.Default => "ayarda yok, koddaki varsayılan",
            ValueSource.Missing => "ayarda yok",
            _ => "düz metin"
        };

        Report.Info($"{label}: {shown} ({origin})");
    }
}
