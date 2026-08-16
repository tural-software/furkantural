namespace FurkanTural_Admin.Models.Log;

public class LogIndexViewModel
{
    public IReadOnlyList<LogAdminDto> Rows { get; set; } = [];
    public int TotalCount { get; set; }
    public DateTime? LastActivityAt { get; set; }

    // Filtreler
    public string? LevelFilter { get; set; }
    public string? SearchProject { get; set; }
    public string? SearchMessage { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }

    // Sayfalama
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalFiltered { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 1;
}
