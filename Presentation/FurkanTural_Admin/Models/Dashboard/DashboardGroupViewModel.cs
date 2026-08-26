namespace FurkanTural_Admin.Models.Dashboard;

public sealed class DashboardGroupViewModel
{
    public required string Name { get; init; }
    public required IReadOnlyList<EntityCardViewModel> Modules { get; init; }
}
