using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Blog'a özgü okumalar; hepsi çoğa-çok kategori bağı ya da liste ekranının tek sorguda dönmesi için var. GetPublishedPageAsync toplam ile sayfayı tek gidiş-dönüşte alır, arama yalnızca başlıkta yapılır (içerik taranmaz), sıralama Id'ye göre azalandır ve sayfa numarası 1 tabanlıdır. GetCategoriesForBlogsAsync liste ekranında blog başına sorgu açılmasın diyedir — hiç kategorisi olmayan blog sözlükte boş liste ile değil, hiç yer almaz; etiket karşılıkları aynı kalıptadır.<para>SetCategoriesAsync ile SetTagsAsync bağların tamamını verilen kümeyle değiştirir: fazlalar kaldırılır, eksikler eklenir, tekrar eden Id'ler teke iner. Ara tablo satırları yumuşak silinmez, gerçekten silinir. Bu metot da kendi başına kaydetmez; <see cref="IUnitOfWork.SaveChangesAsync"/> beklenir.</para></summary>
public interface IBlogRepository : IRepository<Blog>
{
    Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(int pageNumber, int pageSize, int? categoryId, int? tagId, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int Id, string? Title, string? Slug, DateTime CreatedAt, DateTime? UpdatedAt)>> GetSitemapDataAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<Category>>> GetCategoriesForBlogsAsync(IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default);
    Task<List<Category>> GetCategoriesByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task<List<int>> GetCategoryIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task SetCategoriesAsync(int blogId, IReadOnlyCollection<int> categoryIds, int? userId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, List<Tag>>> GetTagsForBlogsAsync(IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetTagsByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task<List<int>> GetTagIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default);
    Task SetTagsAsync(int blogId, IReadOnlyCollection<int> tagIds, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Verilen etiketlerin her birine bağlı yayındaki yazı sayısı. Yönetim listesi ve etiket bulutu bunu tek okumada alır; etiket başına ayrı sayım, listenin kendisi kadar sorgu açardı. Hiç yazısı olmayan etiket sözlükte sıfırla değil, hiç yer almaz.</summary>
    Task<Dictionary<int, int>> GetPostCountsForTagsAsync(IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default);

    /// <summary>Yazının okunma sayısını bir artırır ve satırın gerçekten güncellenip güncellenmediğini döndürür. Varlık yüklenmez: tek bir güncelleme deyimi çalışır, dolayısıyla aynı anda okuyan iki kişi birbirinin artışını ezemez ve sayaç için tabloya ikinci bir gidiş dönüş yapılmaz.<para>Yalnızca yayındaki satırı günceller; pasife alınmış ya da silinmiş bir yazının sayacı, adresine elle istek gönderilerek şişirilemez.</para><para>Bu metot kendi kaydını kendi yapar — <see cref="IUnitOfWork.SaveChangesAsync"/> beklemez — çünkü değişiklik izleyicisinden geçmez.</para></summary>
    Task<bool> IncrementViewCountAsync(int blogId, CancellationToken cancellationToken = default);
}
