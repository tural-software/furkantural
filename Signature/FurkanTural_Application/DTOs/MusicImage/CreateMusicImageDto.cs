namespace FurkanTural_Application.DTOs.MusicImage;

public class CreateMusicImageDto
{
    public string? Url { get; set; }
    public int MusicId { get; set; }
    public int? CreatedBy { get; set; }
}