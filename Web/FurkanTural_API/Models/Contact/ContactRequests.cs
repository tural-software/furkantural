namespace FurkanTural_API.Models.Contact;

public class SubmitContactRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public string? TurnstileToken { get; set; }
}
