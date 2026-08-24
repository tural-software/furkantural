using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir <see cref="MailTemplateType"/> için gönderilecek postanın konusu ve gövdesi. İkisi de yer tutucu taşıyabilir: gövde kadar konu da türün DTO'suyla doldurulur, bu yüzden konu koda gömülü değildir ve panelden değiştirilebilir.<para>AppSourceId şablonu bir sunum projesine bağlar; boş bırakılırsa şablon tüm projeler için geçerli genel sürümdür. Gönderim önce isteği yapan projenin kendi şablonunu arar, bulamazsa genel sürüme düşer — aynı posta türünün Chatural'da ve portfolyoda farklı görünmesi bu sayede mümkündür, her tür için her projeye satır açma zorunluluğu doğurmadan.</para><para>Tür ve proje çifti başına yalnızca <b>bir</b> şablon etkin olabilir; kısıt veri tabanındadır. Pasif satır sayısı sınırsızdır, dolayısıyla taslak tutmak serbesttir — ikinci bir şablonu etkinleştirmeye çalışmak taslağı yayına almadan önce mevcut olanı pasife almayı gerektirir.</para><para>FileName hiçbir zaman okunmaz, dosyadan şablon yüklenmez: HTML'in hangi kaynak dosyadan geldiğini insan için not eden bir etikettir.</para></summary>
public class MailTemplate : BaseEntity
{
    public int MailTemplateTypeId { get; set; }
    public int? AppSourceId { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? FileName { get; set; }
}
