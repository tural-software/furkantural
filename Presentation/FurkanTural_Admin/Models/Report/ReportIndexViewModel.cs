namespace FurkanTural_Admin.Models.Report;

public sealed class ReportIndexViewModel
{
    public IReadOnlyList<ReportAdminDto> Rows { get; init; } = [];
    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
    public int ResolvedCount { get; init; }
    public int DeletedCount { get; init; }

    public string? Search { get; init; }
    public string? TypeFilter { get; init; }
    public string? StatusFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}