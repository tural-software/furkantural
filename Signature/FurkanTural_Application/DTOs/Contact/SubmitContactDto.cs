namespace FurkanTural_Application.DTOs.Contact;

/// <summary>
/// Herkese açık iletişim formunun girdisi. <see cref="CreateContactDto"/> ile karıştırılmamalı: ikisi
/// aynı kaydı üretir ama bu, dışarıdan gelen ham istektir ve bot doğrulaması ister; IpAddress ile
/// UserAgent burada yoktur, onları sunucu isteğin kendisinden okuyup diğerine geçirir.
/// </summary>
public class SubmitContactDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public string? TurnstileToken { get; set; }
}