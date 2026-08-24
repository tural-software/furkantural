using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir <see cref="MailTemplateType"/> için gönderilecek postanın konusu ve gövdesi. İkisi de yer tutucu taşıyabilir: gövde kadar konu da türün DTO'suyla doldurulur, bu yüzden konu koda gömülü değildir ve panelden değiştirilebilir.<para>Tür başına yalnızca <b>bir</b> şablon etkin olabilir; kısıt veri tabanındadır (bkz. <see cref="Constants.MailTemplateDefinitions"/> tohumlarına bağlı yapılandırma). Pasif satır sayısı sınırsızdır, dolayısıyla taslak tutmak serbesttir — ikinci bir şablonu etkinleştirmeye çalışmak taslağı yayına almadan önce mevcut olanı pasife almayı gerektirir.</para><para>FileName hiçbir zaman okunmaz, dosyadan şablon yüklenmez: HTML'in hangi kaynak dosyadan geldiğini insan için not eden bir etikettir.</para></summary>
public class MailTemplate : BaseEntity
{
    public int MailTemplateTypeId { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? FileName { get; set; }
}
