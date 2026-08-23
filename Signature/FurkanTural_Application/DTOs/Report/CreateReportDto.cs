namespace FurkanTural_Application.DTOs.Report;

/// <summary>Şikayet oluşturma. Şikayet eden kimlik token'dan alınır, gövdede yer almaz. TargetType <see cref="FurkanTural_Domain.Constants.ReportDefinitions.TargetTypes"/> değerlerinden biri olmalıdır ve hangi tablonun kaydına bakıldığını yalnızca o belirler; TargetId'nin foreign key'i yoktur ve var olup olmadığı hiç denetlenmez. ReportedUserId ise isteğe bağlıdır ama verilirse doğrulanır — kullanıcı yoksa 404, kendini şikayet girişimi ise reddedilir. Durum istemciden alınmaz, daima Pending başlar.</summary>
public class CreateReportDto
{
    public string TargetType { get; set; } = "User";
    public int? TargetId { get; set; }
    public int? ReportedUserId { get; set; }
    public string? Reason { get; set; }
}
