namespace FurkanTural_Admin.Models.Dashboard;

public sealed class EntityCardViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string IconKey { get; init; }
    public required IReadOnlyList<EntityAction> Actions { get; init; }
    public int? TotalCount { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public bool IsLocked { get; init; } = true;
    public string? ManageUrl { get; init; }
    public string CountUnitLabel { get; init; } = "kayıt";
}
