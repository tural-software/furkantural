namespace FurkanTural_Application.DTOs.Subscriber;

public class AdminSubscriberDto
{
    public int Id { get; set; }
    public string? Email { get; set; }

    /// <summary>Adresin sahibinin doğrulama bağlantısına tıkladığı an. Boşsa kayıt vardır ama abonelik yoktur — gönderim yalnızca dolu olanlara yapılır.</summary>
    public DateTime? ConfirmedAt { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}