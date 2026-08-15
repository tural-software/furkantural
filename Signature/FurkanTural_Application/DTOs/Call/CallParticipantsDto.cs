namespace FurkanTural_Application.DTOs.Call;

public class CallParticipantsDto
{
    public int Id { get; set; }
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    public string? CallType { get; set; }
    public string? Status { get; set; }
}