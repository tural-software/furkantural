using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

/// <summary>E-posta şablonlarının ve bülten gövdelerinin önizlemesini ayrı bir belge olarak sunar. E-posta HTML'i doğası gereği satır içi stil taşır; panelin içerik güvenliği kuralı ise satır içi stile hiç izin vermez. Önizleme panelin içine gömülü (srcdoc) olsaydı o kuralı miras alır ve stilsiz görünürdü.<para>Buradaki belge panelin kuralını değil kendi kuralını taşır: betik hiç çalışmaz, form gönderilemez, dış kaynaktan yalnızca görsel ve yazı tipi yüklenir ve belge sandbox içinde, panelin kökenine erişemeyen ayrı bir köken olarak açılır. Satır içi stile izin veren tek yanıt budur ve yalnızca yönetici oturumuyla açılır.</para></summary>
public class PreviewController(IMailTemplateApiClient mailTemplateApiClient, INewsletterIssueApiClient newsletterIssueApiClient) : Controller
{
    public const string DocumentPolicy =
        "default-src 'none'; style-src 'unsafe-inline'; img-src https: data:; font-src https: data:; " +
        "frame-ancestors 'self'; base-uri 'none'; form-action 'none'; sandbox";

    private readonly IMailTemplateApiClient _mailTemplateApiClient = mailTemplateApiClient;
    private readonly INewsletterIssueApiClient _newsletterIssueApiClient = newsletterIssueApiClient;

    [HttpGet("/Preview/MailTemplate/{id:int}")]
    public async Task<IActionResult> MailTemplate(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        return Document(await _mailTemplateApiClient.GetHtmlContentAsync(id, token, cancellationToken));
    }

    [HttpGet("/Preview/NewsletterIssue/{id:int}")]
    public async Task<IActionResult> NewsletterIssue(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        return Document(await _newsletterIssueApiClient.GetBodyAsync(id, token, cancellationToken));
    }

    private IActionResult Document(string? html)
    {
        if (html is null)
            return NotFound();

        Response.Headers.ContentSecurityPolicy = DocumentPolicy;
        Response.Headers.XFrameOptions = "SAMEORIGIN";
        Response.Headers.CacheControl = "no-store";
        return Content(html, "text/html; charset=utf-8");
    }
}
