using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Portfolio.Helpers;

/// <summary>Proje açıklaması Markdown olarak saklanır ve buradan HTML'e çevrilir. Çıktı doğrudan sayfaya basıldığı için boru hattının en önemli ayarı ham HTML'in kapatılmasıdır: açıklamadaki etiketler kaçışlanır, yani gövdeye gömülen betik çalışmaz. Bu ayar kaldırılırsa içerik girişi tek adımda betik çalıştırma yetkisine dönüşür.<para>Yumuşak satır sonları zorlu satır sonu sayılır. Markdown'ın kendi kuralı bunları birleştirir; burada korunmalarının sebebi Markdown'dan önce yazılmış düz metin açıklamaların görünümünü bozmamaktır.</para><para>Ham HTML'i kapatmak bağlantı adreslerini süzmez: <c>[tıkla](javascript:...)</c> Markdown'ın kendi söz dizimidir ve etiket kaçışından geçer. Bu yüzden belge çözümlendikten sonra her bağlantı ve görsel adresi denetlenir; http, https, mailto ya da göreli olmayan adres <c>#</c> ile değiştirilir.</para><para>Blog projesindeki eşiyle aynı kuralları uygular; ikisi ayrışırsa aynı içerik iki sitede farklı biçimlenir.</para></summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .UseSoftlineBreakAsHardlineBreak()
        .DisableHtml()
        .Build();

    private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

    private const string NeutralUrl = "#";

    public static IHtmlContent ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return HtmlString.Empty;

        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
            if (!IsSafeUrl(link.Url))
                link.Url = NeutralUrl;

        foreach (var link in document.Descendants<AutolinkInline>())
            if (!IsSafeUrl(link.Url))
                link.Url = NeutralUrl;

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        return new HtmlString(writer.ToString());
    }

    private static bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var cleaned = new string(url.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());

        var colon = cleaned.IndexOf(':');
        if (colon < 0)
            return true;

        var boundary = cleaned.IndexOfAny(['/', '?', '#']);
        if (boundary >= 0 && boundary < colon)
            return true;

        return AllowedSchemes.Contains(cleaned[..colon], StringComparer.OrdinalIgnoreCase);
    }
}
