using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>
/// Blog'a özgü okumalar; hepsi çoğa-çok kategori bağı ya da liste ekranının tek sorguda dönmesi için
/// var. GetPublishedPageAsync toplam ile sayfayı tek gidiş-dönüşte alır, arama yalnızca başlıkta yapılır
/// (içerik taranmaz), sıralama Id'ye göre azalandır ve sayfa numarası 1 tabanlıdır.
/// GetCategoriesForBlogsAsync liste ekranında blog başına sorgu açılmasın diyedir — hiç kategorisi
/// olmayan blog sözlükte boş liste ile değil, hiç yer almaz.
///
/// SetCategoriesAsync bağların tamamını verilen kümeyle değiştirir: fazlalar kaldırılır, eksikler
/// eklenir, tekrar eden Id'ler teke iner. Ara tablo satırları yumuşak silinmez, gerçekten silinir. Bu
/// metot da kendi başına kaydetmez; <see cref="IUnitOfWork.SaveChangesAsync"/> beklenir.
/// </summary>
public interface IBlogRepository : IRepository<Blog>
{
    Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int Id, DateTime CreatedAt, DateTime? UpdatedAt)>> GetSitemapDataAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<Category>>> GetCategoriesForBlogsAsync(IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default);
    Task<List<Category>> GetCategoriesByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task<List<int>> GetCategoryIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task SetCategoriesAsync(int blogId, IReadOnlyCollection<int> categoryIds, int? userId, CancellationToken cancellationToken = default);
}