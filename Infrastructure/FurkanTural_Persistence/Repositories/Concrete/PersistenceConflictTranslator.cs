using System.Text.RegularExpressions;
using FurkanTural_Application.Exceptions;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>SQL Server hata numaralarını <see cref="PersistenceConflictException"/> soyuna çevirir. Ayrı durmasının sebebi sınanabilirlik: <c>SqlException</c> dışarıdan üretilemediği için çeviri <see cref="UnitOfWork"/> içinde kalsaydı yalnızca canlı bir veri tabanıyla doğrulanabilirdi.<para>Çevrilen üç numara dışındaki her şey <c>null</c> döner ve olduğu gibi yükselir. Örneğin 8152 (metin kolona sığmadı) bilerek dışarıdadır: o bir çekişme değil, kolon genişliğiyle uyuşmayan bir kod hatasıdır ve 500 olarak gürültü çıkarması istenir.</para></summary>
public static partial class PersistenceConflictTranslator
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueOrPrimaryKeyViolation = 2627;
    private const int ForeignKeyViolation = 547;

    public static PersistenceConflictException? Translate(int errorNumber, string? message, Exception innerException)
    {
        var constraint = ExtractConstraintName(message);
        return errorNumber switch
        {
            UniqueIndexViolation or UniqueOrPrimaryKeyViolation => new DuplicateEntityException(constraint, innerException),
            ForeignKeyViolation => new RelatedEntityMissingException(constraint, innerException),
            _ => null
        };
    }

    /// <summary>Kısıt adını SQL Server'ın hata metninden söker. Ad tırnak içinde gelir ama tırnak türü mesaja göre değişir, ayrıca aynı metinde tablo adı da tırnaklıdır; bu yüzden desen tırnağı tek başına değil "index" ya da "constraint" kelimesinin ardından arar. Metin tanınmazsa <c>null</c> döner ve çeviri yine yapılır — ad tanıya yarar, kararı belirlemez.</summary>
    private static string? ExtractConstraintName(string? message)
        => message is not null && ConstraintName().Match(message) is { Success: true } match
            ? match.Groups[1].Value
            : null;

    [GeneratedRegex(@"(?:index|constraint)\s+['""]([^'""]+)['""]", RegexOptions.IgnoreCase)]
    private static partial Regex ConstraintName();
}
