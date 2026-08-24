using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Gönderilebilecek posta türlerinin sözlüğü; <see cref="MailTemplate"/> bir satırına bağlanır. Kod tarafı türü Id ile değil <see cref="Constants.MailTemplateDefinitions"/> sabitleriyle çözer.<para>Tohumla gelen türlerin karşılığında bir posta DTO'su vardır ve o DTO şablonda kullanılabilecek yer tutucuların tek kaynağıdır. Yönetici panelinden yeni tür eklenebilir, ama eklenen türün DTO'su ve onu gönderen bir çağıranı olmadığı için şablonu kendiliğinden postaya dönüşmez.</para></summary>
public class MailTemplateType : BaseEntity
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
