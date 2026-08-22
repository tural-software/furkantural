namespace FurkanTural_Chat.Models.Chat;

/// <summary>
/// Burada kasıtlı olarak bulunmayan bir alan var: kullanıcının jetonu. Tarayıcıya hiç
/// gönderilmez, oturumda kalır ve isteklere ters vekil tarafından eklenir. Bu modele bir jeton
/// alanı eklemek o kurulumu tek satırda geçersiz kılar.
/// </summary>
public class ChatPageViewModel
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Kullanıcı güncel üyelik sözleşmesini kabul etmiş mi? false ise zorunlu onay modalı gösterilir.</summary>
    public bool AgreementAccepted { get; set; }
}