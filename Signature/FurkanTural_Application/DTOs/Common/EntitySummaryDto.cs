namespace FurkanTural_Application.DTOs.Common;

/// <summary>Yönetim panelindeki varlık kartlarının özeti. Adı yanıltıcıdır: LastActivityAt bir etkinlik kaydı değil, tablodaki en yeni zaman damgasıdır — her satır için UpdatedAt, yoksa DeletedAt, o da yoksa CreatedAt alınıp en büyüğü seçilir. TotalCount de genelde filtresizdir, yani silinmiş ve pasif satırları kapsar; tek istisna kayıt defteridir, orada iki alan da yalnızca canlı satırları görür.</summary>
public sealed record EntitySummaryDto(int TotalCount, DateTime? LastActivityAt);
