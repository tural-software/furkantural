namespace FurkanTural_Application.Exceptions;

/// <summary>Veri tabanının bir kısıtı reddettiği yazma girişimi. Kaydın önüne konan denetimler yarışı kapatamaz — iki istek aynı anda denetimden geçip ikisi de yazmaya gidebilir — bu yüzden son sözü indeks söyler; bu tür de o sözü çağıranın anlayabileceği bir biçime çevirir.<para>ConstraintName tanıya yarar ve istemciye <b>çıkmamalıdır</b>: indeks adı şema bilgisidir. API tarafında istisna sabit metinli bir yanıta çevrilir, ad yalnızca kayda yazılır.</para></summary>
public abstract class PersistenceConflictException(string message, string? constraintName, Exception innerException)
    : Exception(message, innerException)
{
    public string? ConstraintName { get; } = constraintName;
}

/// <summary>Tekil indeks ya da birincil anahtar ihlali (SQL Server 2601 ve 2627). Kayıt akışlarındaki "önce ara, yoksa ekle" deseninin yarış hâlindeki karşılığı budur.</summary>
public sealed class DuplicateEntityException(string? constraintName, Exception innerException)
    : PersistenceConflictException($"Tekil kısıt ihlali: {constraintName ?? "adı çözümlenemedi"}", constraintName, innerException);

/// <summary>Yabancı anahtar ihlali (SQL Server 547). Bağlanılan satır hiç yoktur ya da denetim ile yazma arasında kalıcı olarak silinmiştir.</summary>
public sealed class RelatedEntityMissingException(string? constraintName, Exception innerException)
    : PersistenceConflictException($"Yabancı anahtar ihlali: {constraintName ?? "adı çözümlenemedi"}", constraintName, innerException);
