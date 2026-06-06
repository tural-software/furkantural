namespace FurkanTural_Admin.Models.Call;

public sealed class CallLogAdminDto
{
    public int Id { get; set; }
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    public string? CallerName { get; set; }
    public string? CalleeName { get; set; }
    public string? CallType { get; set; }
    public string? Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
