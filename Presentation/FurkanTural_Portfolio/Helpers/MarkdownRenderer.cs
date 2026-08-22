using Markdig;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Portfolio.Helpers;

/// <summary>
/// Proje açıklaması Markdown olarak saklanır ve buradan HTML'e çevrilir. Çıktı doğrudan sayfaya
/// basıldığı için boru hattının en önemli ayarı ham HTML'in kapatılmasıdır: açıklamadaki etiketler
/// kaçışlanır, yani gövdeye gömülen betik çalışmaz. Bu ayar kaldırılırsa içerik girişi tek adımda
/// betik çalıştırma yetkisine dönüşür.
///
/// Yumuşak satır sonları zorlu satır sonu sayılır. Markdown'ın kendi kuralı bunları birleştirir;
/// burada korunmalarının sebebi Markdown'dan önce yazılmış düz metin açıklamaların görünümünü
/// bozmamaktır.
///
/// Blog projesindeki eşiyle aynı kuralları uygular; ikisi ayrışırsa aynı içerik iki sitede farklı
/// biçimlenir.
/// </summary>
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
}