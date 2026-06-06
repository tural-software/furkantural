namespace FurkanTural_Application.DTOs.Call;

/// <summary>Hub'ın bir aramayı yetkilendirip yönlendirmesi için gereken asgari bilgi.</summary>
public class CallParticipantsDto
{
    public int Id { get; set; }
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    public string? CallType { get; set; }
    public string? Status { get; set; }
}
