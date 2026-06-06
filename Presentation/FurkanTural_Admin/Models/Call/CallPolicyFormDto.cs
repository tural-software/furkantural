namespace FurkanTural_Admin.Models.Call;

public sealed class CallPolicyFormDto
{
    public bool BitrateLimitEnabled { get; set; } = true;
    public int MaxVideoBitrateKbps { get; set; } = 600;
    public int MaxWidth { get; set; } = 640;
    public int MaxHeight { get; set; } = 480;
    public int MaxFps { get; set; } = 24;
}
