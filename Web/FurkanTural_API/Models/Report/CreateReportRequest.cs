namespace FurkanTural_API.Models.Report;

public class CreateReportRequest
{
    /// <summary>"User" | "Message" | "Media" | "Call".</summary>
    public string TargetType { get; set; } = "User";
    public int? TargetId { get; set; }
    public int? ReportedUserId { get; set; }
    public string? Reason { get; set; }
}