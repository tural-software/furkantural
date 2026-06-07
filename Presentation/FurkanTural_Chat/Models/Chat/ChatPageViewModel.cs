namespace FurkanTural_Chat.Models.Chat;

public class ChatPageViewModel
{
    // Not: Kullanıcı JWT'si artık tarayıcıya gönderilmez (BFF/YARP proxy ekler). Token view'e taşınmaz.
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Kullanıcı güncel üyelik sözleşmesini kabul etmiş mi? false ise zorunlu onay modalı gösterilir.</summary>
    public bool AgreementAccepted { get; set; }
}
