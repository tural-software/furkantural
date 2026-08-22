using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Portfolio.Models;

public sealed class ContactFormModel
{
    [Required]
    [MaxLength(200)]
    public string? Name { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(2000)]
    public string? Message { get; set; }

    [Required]
    public string? TurnstileToken { get; set; }
}