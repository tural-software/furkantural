namespace FurkanTural_Application.DTOs.MusicImage;

/// <summary>Müzik kaydının görseli. IsCover tekil değildir: yeni bir görseli kapak işaretlemek eskisinin işaretini kaldırmaz, hiçbir yerde teklik denetimi yapılmaz.</summary>
public class MusicImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int MusicId { get; set; }
}
