namespace FurkanTural_Application.DTOs.Music;

public class UpdateMusicDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Artist { get; set; }
    public string? Productor { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Lyrics { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? YouTubeMusicUrl { get; set; }
    public int? UpdatedBy { get; set; }
}