namespace FurkanTural_Application.DTOs.Music;

public class CreateMusicDto
{
    public string? Name { get; set; }
    public string? Artist { get; set; }
    public string? Productor { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Lyrics { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? CreatedBy { get; set; }
}