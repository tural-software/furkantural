using FurkanTural_Application.DTOs.Retention;

namespace FurkanTural_Application.Repositories.Abstract;

public sealed record DataRetentionPurge(IReadOnlyList<DataRetentionCategoryDto> Categories, IReadOnlyList<string> Files);

/// <summary>Saklama süresi dolmuş kişisel veriyi sayar ve siler. İki metot aynı kuralları uygular; önizlemede görülen sayı silmede silinecek olandır.<para>Silme tek transaction içinde yapılır: ya hepsi silinir ya hiçbiri. Diskteki dosyalar transaction'a katılamaz, bu yüzden silinmiş satırların dosya yolları çağırana döndürülür ve dosyalar ancak satırlar kalıcı olarak silindikten sonra kaldırılır.</para></summary>
public interface IDataRetentionStore
{
    Task<IReadOnlyList<DataRetentionCategoryDto>> CountAsync(DateTime cutoff, DateTime staleBefore, CancellationToken cancellationToken = default);

    Task<DataRetentionPurge> PurgeAsync(DateTime cutoff, DateTime staleBefore, CancellationToken cancellationToken = default);
}
