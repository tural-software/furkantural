namespace FurkanTural_Application.DTOs.MusicImage;

public class CreateMusicImageDto
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int MusicId { get; set; }
    public int? CreatedBy { get; set; }
}