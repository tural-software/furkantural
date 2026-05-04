namespace FurkanTural_Application.DTOs.User;

public class UpdateUserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? UpdatedBy { get; set; }
}