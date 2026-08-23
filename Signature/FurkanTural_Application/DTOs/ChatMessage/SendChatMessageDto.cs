namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>Mesaj gönderme girdisi. Gönderen alanı yoktur, kimlik token'dan alınır. Content kırpıldıktan sonra en fazla 4000 karakter olabilir; aşan istek kaydedilmez, hata döner.</summary>
public class SendChatMessageDto
{
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
}
