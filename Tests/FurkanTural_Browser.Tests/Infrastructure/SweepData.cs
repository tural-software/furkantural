namespace FurkanTural_Browser.Tests.Infrastructure;

public static class SweepData
{
    public static SitePage Page(string id) =>
        SiteMap.Pages.FirstOrDefault(p => p.Id == id)
        ?? throw new ArgumentException($"Bilinmeyen sayfa: {id}", nameof(id));

    public static Viewport Screen(string name) =>
        Viewport.All.FirstOrDefault(v => v.Name == name)
        ?? throw new ArgumentException($"Bilinmeyen görünüm: {name}", nameof(name));

    public static TheoryData<string, string> EveryPageEveryWidth()
    {
        var data = new TheoryData<string, string>();
        foreach (var page in SiteMap.Pages)
            foreach (var viewport in Viewport.All)
                data.Add(page.Id, viewport.Name);
        return data;
    }

    public static TheoryData<string> EveryPage()
    {
        var data = new TheoryData<string>();
        foreach (var page in SiteMap.Pages) data.Add(page.Id);
        return data;
    }

    public static TheoryData<string, string> EveryPageBothThemes()
    {
        var data = new TheoryData<string, string>();
        foreach (var page in SiteMap.Pages)
            foreach (var theme in Themes.Both)
                data.Add(page.Id, theme);
        return data;
    }
}
