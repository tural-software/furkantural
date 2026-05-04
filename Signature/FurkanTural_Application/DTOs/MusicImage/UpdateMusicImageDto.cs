namespace FurkanTural_Application.DTOs.MusicImage;

public class UpdateMusicImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public int MusicId { get; set; }
    public int? UpdatedBy { get; set; }
}