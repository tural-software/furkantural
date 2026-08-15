namespace FurkanTural_Application.DTOs.Contact;

public class CreateContactDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}