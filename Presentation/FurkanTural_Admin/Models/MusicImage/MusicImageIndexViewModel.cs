namespace FurkanTural_Admin.Models.MusicImage;

public sealed class MusicImageIndexViewModel
{
    public IReadOnlyList<MusicImageAdminDto> Rows { get; set; } = [];
    public int TotalCount   { get; set; }
    public int ActiveCount  { get; set; }
    public int PassiveCount { get; set; }
    public int DeletedCount { get; set; }

    // Filters
    public string? SearchUrl     { get; set; }
    public string? IsCoverFilter { get; set; }
    public string? ActiveFilter  { get; set; }
    public string? DeletedFilter { get; set; }
    public int?    MusicIdFilter { get; set; }
    public string? DateFrom      { get; set; }
    public string? DateTo        { get; set; }

    // Pagination
    public int PageNumber    { get; set; } = 1;
    public int PageSize      { get; set; } = 10;
    public int TotalFiltered { get; set; }
    public int TotalPages    => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 1;
}
