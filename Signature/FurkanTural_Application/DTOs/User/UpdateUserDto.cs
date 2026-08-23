namespace FurkanTural_Application.DTOs.User;

/// <summary>Kullanıcı güncelleme. Password bu alanların tek istisnasıdır: boş bırakıldığında mevcut parola korunur, yalnızca dolu geldiğinde hash'lenip yazılır. Diğer alanlar koşulsuz üzerine yazılır, yani eksik gönderilen bir alan güncellenmez değil, boşaltılır.</summary>
public class UpdateUserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public int? UpdatedBy { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
