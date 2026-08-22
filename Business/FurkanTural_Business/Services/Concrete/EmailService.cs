using System.Net;
using System.Net.Mail;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// SMTP ile HTML e-posta gönderimi. <see cref="FurkanTural_Application.Wrappers.Result"/> zarfı
/// kullanılmaz; gönderim başarısızsa istisna fırlar ve çağıran ne yapacağına kendi karar verir.
///
/// Ayarlar <c>Smtp</c> bölümünden okunur ve her değer şifrelenmiş olabilir: beklenen desene uyanlar
/// çözülür, uymayanlar olduğu gibi kullanılır. Host, port, kullanıcı ve gönderen adresi için koda
/// gömülü varsayılanlar vardır — yalnızca parolanın karşılığı yoktur, o eksikse gönderim yapılmaz.
/// </summary>
public class EmailService(IConfiguration configuration, IEncryptionService encryptionService) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IEncryptionService _encryptionService = encryptionService;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = Decrypt(_configuration["Smtp:Host"]) ?? "smtp.hostinger.com";
        var portStr = Decrypt(_configuration["Smtp:Port"]) ?? "587";
        var username = Decrypt(_configuration["Smtp:Username"]) ?? "messanger@furkantural.com";
        var password = Decrypt(_configuration["Smtp:Password"]) ?? throw new InvalidOperationException("SMTP password not configured.");
        var fromEmail = Decrypt(_configuration["Smtp:From"]) ?? username;
        var fromName = _configuration["Smtp:FromName"] ?? "Furkan Tural";
        var port = int.TryParse(portStr, out var p) ? p : 587;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, cancellationToken);
    }

    private string? Decrypt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var parts = value.Split(':');
        if (parts.Length != 3) return value;
        var result = _encryptionService.Decrypt(value);
        return result.Success ? result.Data : value;
    }
}