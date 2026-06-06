namespace FurkanTural_API.Models.Message;

public class SendAudioRequest
{
    public int ReceiverId { get; set; }
    /// <summary>Ses verisi (base64 kodlanmış byte dizisi).</summary>
    public byte[] AudioData { get; set; } = [];
    /// <summary>Uzantı tespiti için dosya adı. Örn: "voice.webm"</summary>
    public string? AudioName { get; set; }
    public int? DurationSeconds { get; set; }
}
