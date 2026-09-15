using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Özellikler her çağrıda yansıma ile okunur. Şablon gövdeleri elle düzenlenen az sayıda satırdır ve posta gönderimi zaten ağ üzerinden yürüdüğü için buradaki yansıma maliyeti ölçülebilir bir yük değildir; karşılığında yer tutucu listesi hiçbir yerde ikinci kez yazılmaz.</summary>
public sealed partial class MailRenderer(ILogger<MailRenderer> logger) : IMailRenderer
{
    private readonly ILogger<MailRenderer> _logger = logger;

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();

    private static readonly HashSet<string> RawHtmlPlaceholders = new(StringComparer.Ordinal)
    {
        $"{nameof(NewsletterIssueMailDto)}.{nameof(NewsletterIssueMailDto.Body)}"
    };

    public string Render(string? template, object payload, bool encodeHtml = false)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var payloadName = payload.GetType().Name;

        var values = payload.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(payload) as string ?? string.Empty, StringComparer.Ordinal);

        var unknown = new HashSet<string>(StringComparer.Ordinal);

        var rendered = PlaceholderPattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (!values.TryGetValue(key, out var value))
            {
                unknown.Add(key);
                return string.Empty;
            }

            return encodeHtml && !RawHtmlPlaceholders.Contains($"{payloadName}.{key}")
                ? HtmlEncoder.Default.Encode(value)
                : value;
        });

        if (unknown.Count > 0)
            _logger.LogWarning("Şablonda karşılığı olmayan yer tutucular boşa indirildi: {Placeholders} ({PayloadType})",
                string.Join(", ", unknown), payload.GetType().Name);

        return rendered;
    }
}
