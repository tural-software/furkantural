using Markdig;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Blog.Helpers;

/// <summary>
/// Blog içeriğini (Markdown) güvenli HTML'e render eder.
/// Ham HTML <see cref="MarkdownExtensions.DisableHtml"/> ile devre dışı bırakıldığından
/// içeriğe gömülü &lt;script&gt; vb. etiketler kaçışlanır → XSS-güvenli.
/// Eski düz-metin kayıtlar da geçerli Markdown'dır; satır sonları korunur.
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

    /// <summary>Markdown'ı düz metne indirger (özet/snippet için); içerik boşsa boş döner.</summary>
    public static string ToPlainText(string? markdown)
        => string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToPlainText(markdown).Trim();
}
