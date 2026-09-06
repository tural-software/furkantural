namespace FurkanTural_Admin.Models.Common;

public sealed class AdminWeeklyCountsModel
{
    public int? Blogs { get; set; }
    public int? Users { get; set; }
    public int? Contacts { get; set; }
    public int? Subscribers { get; set; }
}

public sealed class AdminDashboardModel
{
    public Dictionary<string, EntitySummaryModel> Summaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int? UnreadContacts { get; set; }
    public int? PendingReports { get; set; }
    public int? ActiveUsers { get; set; }
    public AdminWeeklyCountsModel? ThisWeek { get; set; }
    public AdminWeeklyCountsModel? LastWeek { get; set; }
}
