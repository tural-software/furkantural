namespace FurkanTural_Application.DTOs.MusicImage;

public class AdminMusicImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public int MusicId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
