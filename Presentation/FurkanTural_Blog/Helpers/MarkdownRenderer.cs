using Markdig;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Blog.Helpers;

/// <summary>Yazı içeriği Markdown olarak saklanır ve buradan HTML'e çevrilir. Çıktı doğrudan sayfaya basıldığı için boru hattının en önemli ayarı ham HTML'in kapatılmasıdır: içerikteki etiketler kaçışlanır, yani bir yazının gövdesine gömülen betik çalışmaz. Bu ayar kaldırılırsa içerik üretimi tek adımda betik çalıştırma yetkisine dönüşür.<para>Yumuşak satır sonları zorlu satır sonu sayılır. Markdown'ın kendi kuralı bunları birleştirir; burada korunmalarının sebebi Markdown'dan önce yazılmış düz metin kayıtların görünümünü bozmamaktır.</para></summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .UseSoftlineBreakAsHardlineBreak()
        .DisableHtml()
        .Build();

    public static IHtmlContent ToHtml(string? markdown)
        => string.IsNullOrWhiteSpace(markdown)
            ? HtmlString.Empty
            : new HtmlString(Markdown.ToHtml(markdown, Pipeline));

    /// <summary>Biçimlendirme atılıp düz metin üretir; sayfada değil, özet ve meta etiketlerinde kullanılır.</summary>
    public static string ToPlainText(string? markdown)
        => string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToPlainText(markdown).Trim();
}
