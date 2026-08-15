namespace FurkanTural_Application.DTOs.Auth;

/// <summary>
/// Başarılı girişin ve uygulama token'ının ortak yanıtı. MembershipAgreementAccepted "onayladı mı"
/// değil "geçerli sürümü onayladı mı" sorusunu yanıtlar: sözleşme sürümü artırıldığında eski onaylar
/// kabul edilmemiş sayılır ve alan bütün kullanıcılar için false'a döner.
/// </summary>
public class LoginResultDto
{
    public string? Token { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? RoleName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool MembershipAgreementAccepted { get; set; }
}