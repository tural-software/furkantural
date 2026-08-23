namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>BaseEntityConfiguration'daki global sorgu süzgecinin ham SQL karşılığı. Dapper EF'in süzgecini tanımadığı için aynı kural iki yerde yaşamak zorundadır; ikisi ayrışırsa aradaki fark derlemede değil yalnızca üretimde görülür.</summary>
internal static class LiveRows
{
    public const string Filter = "IsDeleted = 0 AND IsActive = 1";

    public static string FilterFor(string alias) => $"{alias}.IsDeleted = 0 AND {alias}.IsActive = 1";
}
