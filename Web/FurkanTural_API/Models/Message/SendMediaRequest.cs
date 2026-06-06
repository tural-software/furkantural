namespace FurkanTural_API.Models.Message;

public class SendMediaRequest
{
    public int ReceiverId { get; set; }
    /// <summary>Medya verisi (base64 kodlanmış byte dizisi).</summary>
    public byte[] Data { get; set; } = [];
    /// <summary>Uzantı tespiti için dosya adı. Örn: "photo.jpg" / "clip.mp4"</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>"Image" | "Video".</summary>
    public string MediaType { get; set; } = "Image";
    /// <summary>Video için süre (saniye); foto için null.</summary>
    public int? DurationSeconds { get; set; }
}
