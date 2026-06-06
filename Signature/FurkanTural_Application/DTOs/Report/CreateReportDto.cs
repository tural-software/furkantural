namespace FurkanTural_Application.DTOs.Report;

public class CreateReportDto
{
    /// <summary>"User" | "Message" | "Media" | "Call".</summary>
    public string TargetType { get; set; } = "User";
    /// <summary>Hedef kaydın Id'si (mesaj/medya/arama için). Kullanıcı şikayetinde null olabilir.</summary>
    public int? TargetId { get; set; }
    /// <summary>Şikayet edilen kullanıcı (varsa).</summary>
    public int? ReportedUserId { get; set; }
    public string? Reason { get; set; }
}
