namespace FurkanTural_API.Models.ProjectImage;

public class CreateProjectImageRequest
{
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi).</summary>
    public byte[] ImageData { get; set; } = [];
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. Örn: "photo.jpg"</summary>
    public string ImageName { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
}

public class UpdateProjectImageRequest
{
    public int Id { get; set; }
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi). Güncellenmeyecekse null bırakın.</summary>
    public byte[]? ImageData { get; set; }
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. ImageData doluysa zorunludur.</summary>
    public string? ImageName { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int ProjectId { get; set; }
}