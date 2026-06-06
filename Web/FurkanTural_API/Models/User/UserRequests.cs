namespace FurkanTural_API.Models.User;

public class CreateUserRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class SeedAdminRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class UpdateUserRequest
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UserAvatarRequest
{
    /// <summary>Avatar görseli (base64 kodlanmış byte dizisi).</summary>
    public byte[] ImageData { get; set; } = [];
    /// <summary>Uzantı tespiti için dosya adı. Örn: "avatar.png"</summary>
    public string? ImageName { get; set; }
}
