namespace FurkanTural_API.Models.Message;

public class SendMessageRequest
{
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
}