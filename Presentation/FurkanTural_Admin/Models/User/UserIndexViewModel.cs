using FurkanTural_Admin.Models.Role;

namespace FurkanTural_Admin.Models.User;

public sealed class UserIndexViewModel
{
    public IReadOnlyList<UserAdminDto> Rows { get; init; } = [];
    public IReadOnlyList<RoleAdminDto> RoleOptions { get; init; } = [];

    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    // Filter state
    public string? SearchUsername { get; init; }
    public int? RoleFilter { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    // Pagination
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
