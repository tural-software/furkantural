namespace FurkanTural_Admin.Models.Role;

public sealed class RoleIndexViewModel
{
    public IReadOnlyList<RoleAdminDto> Rows { get; init; } = [];

    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    public string? SearchName { get; init; }
    public int? RoleIdFilter { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}