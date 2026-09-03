namespace FurkanTural_Admin.Models.Log;

public class LogAdminDto
{
    public int Id { get; set; }
    public string? Project { get; set; }
    public DateTime Date { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? Path { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}