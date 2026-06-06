namespace FurkanTural_API.Models.Report;

public class UpdateReportStatusRequest
{
    /// <summary>Pending/Reviewed/Dismissed/ActionTaken.</summary>
    public string Status { get; set; } = "Reviewed";
    public string? AdminNote { get; set; }
}
