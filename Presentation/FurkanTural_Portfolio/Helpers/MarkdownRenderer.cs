using Markdig;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Portfolio.Helpers;

/// <summary>
/// Proje açıklamasını (Markdown) güvenli HTML'e render eder.
/// Ham HTML <see cref="MarkdownExtensions.DisableHtml"/> ile devre dışı bırakıldığından
/// içeriğe gömülü &lt;script&gt; vb. etiketler kaçışlanır → XSS-güvenli.
/// Eski düz-metin açıklamalar da geçerli Markdown'dır; satır sonları korunur.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()                     // çıplak URL'ler tıklanabilir linke dönüşür
        .UsePipeTables()                    // | tablo | desteği
        .UseSoftlineBreakAsHardlineBreak()  // tek satır sonu → <br> (eski metinlerin görünümü korunur)
        .DisableHtml()                      // ham HTML'i kaçışla (XSS koruması)
        .Build();

    /// <summary>Markdown'ı HTML'e çevirir; içerik boşsa boş içerik döner.</summary>
    public static IHtmlContent ToHtml(string? markdown)
        => string.IsNullOrWhiteSpace(markdown)
            ? HtmlString.Empty
            : new HtmlString(Markdown.ToHtml(markdown, Pipeline));
}
