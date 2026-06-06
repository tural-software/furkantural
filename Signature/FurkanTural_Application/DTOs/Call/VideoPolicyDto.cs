namespace FurkanTural_Application.DTOs.Call;

/// <summary>İstemciye giden efektif video politikası (kullanıcıya özel hesaplanır).</summary>
public class VideoPolicyDto
{
    public bool Enabled { get; set; }
    public int MaxBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}
