namespace FurkanTural_Application.DTOs.MusicImage;

public class MusicImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int MusicId { get; set; }
}