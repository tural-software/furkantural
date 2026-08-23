namespace FurkanTural_Application.DTOs.Education;

/// <summary>Portfolyodaki eğitim kaydı. EndDate boş bırakılması eksik veri değil "hâlâ devam ediyor" anlamına gelir; görünüm katmanı bu durumda tarih yerine sürüyor ifadesi basar.</summary>
public class EducationDto
{
    public int Id { get; set; }
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
