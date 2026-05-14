namespace FurkanTural_Admin.Models.Project;

public sealed class ProjectIndexViewModel
{
    public IReadOnlyList<ProjectAdminDto> Rows { get; init; } = [];

    public int TotalCount   { get; init; }
    public int ActiveCount  { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    // Filters
    public string? SearchTitle      { get; init; }
    public string? CompletedFilter  { get; init; }
    public string? ActiveFilter     { get; init; }
    public string? DeletedFilter    { get; init; }
    public int?    ProjectIdFilter  { get; init; }
    public string? DateFrom         { get; init; }
    public string? DateTo           { get; init; }

    // Pagination
    public int PageNumber    { get; init; } = 1;
    public int PageSize      { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages    => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
