namespace FurkanTural_Application.DTOs.Common;

/// <summary>Panelin dört haftalık sayacı: son yedi günde (ya da istenen pencerede) eklenen blog yazısı, kullanıcı, iletişim mesajı ve abone; silinmişler sayılmaz.</summary>
public sealed record AdminWeeklyCountsDto(int Blogs, int Users, int Contacts, int Subscribers);

/// <summary>Yönetim panelinin açılış ekranı için tek yanıt: yirmi bir varlığın özeti (anahtar, uçların yol adıdır: blog, blogimage, user, log …), okunmamış iletişim ve bekleyen şikayet sayısı, pencere içinde görülen aktif kullanıcı sayısı ve iki haftalık sayaç. Panel önceden bunları otuz iki ayrı istekle topluyordu; bu uç aynı bilgiyi tek gidiş-dönüşte verir, tek tek uçlar yine yerinde durur.</summary>
public sealed record AdminDashboardDto(
    IReadOnlyDictionary<string, EntitySummaryDto> Summaries,
    int UnreadContacts,
    int PendingReports,
    int ActiveUsers,
    AdminWeeklyCountsDto ThisWeek,
    AdminWeeklyCountsDto LastWeek);
