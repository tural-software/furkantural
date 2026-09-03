namespace FurkanTural_Admin.Models.Report;

public sealed class ReportAdminDto
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public int? ReportedUserId { get; set; }
    public string? ReportedUserName { get; set; }
    public string? TargetType { get; set; }
    public int? TargetId { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public string? AdminNote { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}