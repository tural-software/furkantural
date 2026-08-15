namespace FurkanTural_Application.DTOs.Experience;

/// <summary>
/// Portfolyodaki iş deneyimi kaydı. EndDate boş bırakılması eksik veri değil "hâlâ sürüyor" anlamına
/// gelir; görünüm katmanı bu durumda tarih yerine sürüyor ifadesi basar.
/// </summary>
public class ExperienceDto
{
    public int Id { get; set; }
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}