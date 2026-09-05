namespace FurkanTural_Admin.Models.Dashboard;

public sealed record KpiViewModel(string Key, string Label, string Value, string? Detail, int? Trend, string? TrendText, string? Url);
