namespace FurkanTural_Admin.Models.Dashboard;

public sealed class DashboardViewModel
{
    public string? Username { get; init; }
    public required IReadOnlyList<DashboardGroupViewModel> Groups { get; init; }
    public int ModuleCount => Groups.Sum(g => g.Modules.Count);
    public int? TotalRecordCount { get; init; }

    public IReadOnlyList<AttentionItemViewModel> Attention { get; init; } = [];

    public IReadOnlyList<KpiViewModel> Kpis { get; init; } = [];
}
