namespace FurkanTural_Application.DTOs.Common;

/// <summary>Toplu işlemin sonucu. Requested istenen tekil kimlik sayısı, Affected gerçekten değişen satır sayısı, Skipped ise bulunamayan ya da işlem için uygun durumda olmayan (silinmişi silmek, silinmemişi geri yüklemek gibi) kimliklerdir. Atlanan kayıt hata değildir; istek tek kayıtta bile başarısız sayılmaz, yalnızca listelenir.</summary>
public sealed record BulkActionResultDto(int Requested, int Affected, IReadOnlyList<int> Skipped);
