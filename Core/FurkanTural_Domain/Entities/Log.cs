using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Uygulama olay kaydı. <see cref="Source"/> kaydı yazan yeri <c>Uygulama-Bileşen-İşlem</c> biçiminde damgalar; tek üretim yeri <see cref="Constants.LogSources"/>tir ve değer oradan geçmeden yazılmaz. Level'ın sabit listesi yoktur; API tarafında ActivityLogger "Information" ya da "Warning" yazar.<para>Sütun 06.09.2026'da <c>Project</c> adından çevrildi: taşıdığı bilgi portfolyodaki proje kaydıyla değil, olayın kaynağıyla ilgiliydi.</para></summary>
public class Log : BaseEntity
{
    public string? Source { get; set; }
    public DateTime Date { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? Path { get; set; }
}
