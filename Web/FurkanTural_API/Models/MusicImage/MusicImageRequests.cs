namespace FurkanTural_API.Models.MusicImage;

public class CreateMusicImageRequest
{
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi).</summary>
    public byte[] ImageData { get; set; } = [];
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. Örn: "cover.png"</summary>
    public string ImageName { get; set; } = string.Empty;
    public int MusicId { get; set; }
}

public class UpdateMusicImageRequest
{
    public int Id { get; set; }
    /// <summary>Görsel verisi (base64 kodlanmış byte dizisi). Güncellenmeyecekse null bırakın.</summary>
    public byte[]? ImageData { get; set; }
    /// <summary>Orijinal dosya adı — uzantı tespiti için kullanılır. ImageData doluysa zorunludur.</summary>
    public string? ImageName { get; set; }
    public int MusicId { get; set; }
}
