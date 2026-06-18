namespace FurkanTural_Portfolio.Models;

// API'den gelen görsel (ProjectImage/MusicImage). Url göreli yol (örn. "projects/images/x.jpg");
// tam adres = ApiBaseUrl + "/" + Url.
public sealed class RemoteImageViewModel
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
}

public sealed class ProjectDetailViewModel
{
    public required ProjectViewModel Project { get; init; }
    public IReadOnlyList<RemoteImageViewModel> Images { get; init; } = [];
    public string ApiBaseUrl { get; init; } = string.Empty;
}

public sealed class SongDetailViewModel
{
    public required SongViewModel Song { get; init; }
    public IReadOnlyList<RemoteImageViewModel> Images { get; init; } = [];
    public string ApiBaseUrl { get; init; } = string.Empty;
}
