namespace FurkanTural_Application.DTOs.Common;

/// <summary>Yönetici listesinin üstündeki dört sayaç. Tek bir gruplu sorgudan gelir, satır taşımaz. Active ve Passive yalnızca silinmemiş satırları sayar; Deleted silinmişleri; Total ise üçünün toplamıdır. Süzgeçle çağrılırsa sayaçlar o süzgecin kümesini anlatır — panel bugün süzgeçsiz çağırıp bütün tabloyu özetler.</summary>
public sealed record AdminStatusCountsDto(int Total, int Active, int Passive, int Deleted);
