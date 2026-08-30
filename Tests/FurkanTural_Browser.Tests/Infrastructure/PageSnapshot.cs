using System.Text.Json;

namespace FurkanTural_Browser.Tests.Infrastructure;

public sealed record Heading(int Level, string Text);

public sealed class PageSnapshot
{
    public required SitePage Page { get; init; }
    public required Viewport Viewport { get; init; }
    public required string Theme { get; init; }
    public required string Url { get; init; }
    public required int Status { get; init; }

    public int ScrollWidth { get; init; }
    public int ClientWidth { get; init; }
    public int Overflow => ScrollWidth - ClientWidth;

    public string Lang { get; init; } = "";
    public string Title { get; init; } = "";
    public string AppliedTheme { get; init; } = "";
    public int H1Count { get; init; }

    public IReadOnlyList<Heading> Headings { get; init; } = [];
    public IReadOnlyList<string> Overflowers { get; init; } = [];
    public IReadOnlyList<string> Scrollers { get; init; } = [];
    public IReadOnlyList<string> PositiveTabindex { get; init; } = [];
    public int MainLandmarks { get; init; }
    public int NavLandmarks { get; init; }
    public bool LoginFormPresent { get; init; }
    public bool AutofocusPresent { get; init; }
    public IReadOnlyList<string> SmallTargets { get; init; } = [];
    public IReadOnlyList<string> MissingAlt { get; init; } = [];
    public IReadOnlyList<string> Unlabelled { get; init; } = [];
    public IReadOnlyList<string> NamelessLinks { get; init; } = [];
    public IReadOnlyList<string> LowContrast { get; init; } = [];
    public IReadOnlyList<string> Unmeasurable { get; init; } = [];
    public IReadOnlyList<string> DuplicateIds { get; init; } = [];
    public IReadOnlyList<string> ConsoleErrors { get; init; } = [];
    public IReadOnlyList<string> FailedRequests { get; init; } = [];

    public string Where => $"{Page.App.Name} {Page.Path} @ {Viewport.Name}/{Theme}";

    public string Report(IReadOnlyList<string> items) =>
        items.Count == 0 ? "" : Environment.NewLine + string.Join(Environment.NewLine, items.Select(i => "  - " + i));

    public static PageSnapshot From(
        SitePage page,
        Viewport viewport,
        string theme,
        string url,
        int status,
        JsonElement probe,
        IReadOnlyList<string> consoleErrors,
        IReadOnlyList<string> failedRequests)
    {
        static IReadOnlyList<string> Strings(JsonElement e, string name) =>
            e.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array
                ? a.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                : [];

        static int Int(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (int)Math.Round(v.GetDouble()) : 0;

        static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        var headings = probe.TryGetProperty("headings", out var hs) && hs.ValueKind == JsonValueKind.Array
            ? hs.EnumerateArray().Select(h => new Heading(Int(h, "level"), Str(h, "text"))).ToArray()
            : [];

        return new PageSnapshot
        {
            Page = page,
            Viewport = viewport,
            Theme = theme,
            Url = url,
            Status = status,
            ScrollWidth = Int(probe, "scrollWidth"),
            ClientWidth = Int(probe, "clientWidth"),
            Lang = Str(probe, "lang"),
            Title = Str(probe, "title"),
            AppliedTheme = Str(probe, "theme"),
            H1Count = Int(probe, "h1Count"),
            Headings = headings,
            Overflowers = Strings(probe, "overflowers"),
            Scrollers = Strings(probe, "scrollers"),
            PositiveTabindex = Strings(probe, "positiveTabindex"),
            MainLandmarks = Int(probe, "mainLandmarks"),
            NavLandmarks = Int(probe, "navLandmarks"),
            LoginFormPresent = probe.TryGetProperty("loginFormPresent", out var lf) && lf.ValueKind == JsonValueKind.True,
            AutofocusPresent = probe.TryGetProperty("autofocusPresent", out var af) && af.ValueKind == JsonValueKind.True,
            SmallTargets = Strings(probe, "smallTargets"),
            MissingAlt = Strings(probe, "missingAlt"),
            Unlabelled = Strings(probe, "unlabelled"),
            NamelessLinks = Strings(probe, "namelessLinks"),
            LowContrast = Strings(probe, "lowContrast"),
            Unmeasurable = Strings(probe, "unmeasurable"),
            DuplicateIds = Strings(probe, "duplicateIds"),
            ConsoleErrors = consoleErrors,
            FailedRequests = failedRequests
        };
    }
}
