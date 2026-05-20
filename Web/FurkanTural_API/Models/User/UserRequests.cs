namespace FurkanTural_API.Models.User;

public class CreateUserRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
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
}
