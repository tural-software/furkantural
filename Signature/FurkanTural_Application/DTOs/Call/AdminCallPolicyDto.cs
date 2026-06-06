namespace FurkanTural_Application.DTOs.Call;

public class AdminCallPolicyDto
{
    public int Id { get; set; }
    public bool BitrateLimitEnabled { get; set; }
    public int MaxVideoBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
