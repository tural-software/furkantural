namespace FurkanTural_API.Models.BlogImage;

public class CreateBlogImageRequest
{
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi).</summary>
    public byte[] ImageData { get; set; } = [];
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. Örn: "photo.jpg"</summary>
    public string ImageName { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}

public class UpdateBlogImageRequest
{
    public int Id { get; set; }
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi). Güncellenmeyecekse null bırakın.</summary>
    public byte[]? ImageData { get; set; }
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. ImageData doluysa zorunludur.</summary>
    public string? ImageName { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}
