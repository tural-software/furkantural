namespace FurkanTural_Domain.Constants;

/// <summary>
/// <see cref="Entities.ChatMessage.MessageType"/> için geçerli değerler. Alan null veya boşsa mesaj
/// <see cref="Text"/> kabul edilir; tür taşımayan eski kayıtlar bu yüzden çalışmayı sürdürür.
/// </summary>
public static class ChatMessageTypes
{
    public const string Text = "Text";
    public const string Audio = "Audio";
    public const string Image = "Image";
    public const string Video = "Video";

    public static bool IsMedia(string? type) =>
        string.Equals(type, Image, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, Video, StringComparison.OrdinalIgnoreCase);
}
