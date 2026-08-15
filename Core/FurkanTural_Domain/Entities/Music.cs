using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Portfolyodaki müzik kaydı; görsellerini <see cref="MusicImage"/> taşır.
/// </summary>
public class Music : BaseEntity
{
    public string? Name { get; set; }
    public string? Artist { get; set; }
    public string? Productor { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Lyrics { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? YouTubeMusicUrl { get; set; }
}