namespace FurkanTural_Application.DTOs.Call;

public class UpdateCallPolicyDto
{
    public bool BitrateLimitEnabled { get; set; }
    public int MaxVideoBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}
