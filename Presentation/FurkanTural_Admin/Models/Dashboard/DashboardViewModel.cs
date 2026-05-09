namespace FurkanTural_Admin.Models.Dashboard;

public sealed class DashboardViewModel
{
    public string? Username { get; init; }
    public required IReadOnlyList<EntityCardViewModel> Modules { get; init; }
}
