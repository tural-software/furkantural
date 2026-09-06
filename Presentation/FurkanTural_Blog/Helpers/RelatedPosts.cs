using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Helpers;

/// <summary>Yazı sayfasının altındaki "İlgili yazılar" seçimi. Adayları ortak kategori sayısına, eşitlikte tarihe göre sıralar.<para>Ayrı bir uç yok ve gerekmiyor: adaylar mevcut <c>blog/paged?categoryId=</c> ucundan gelir ve her aday kendi kategorilerini taşıdığı için sıralama istemcide yapılabilir.</para></summary>
public static class RelatedPosts
{
    /// <summary>Gösterilecek kart sayısı. Aday sayfası bundan bir fazla istenir, çünkü yazının kendisi de kendi kategorisinin listesinde döner ve elenir.</summary>
    public const int Count = 3;

    public static int CandidatePageSize => Count + 1;

    /// <summary>Ortak kategorisi olmayan aday listeye girmez. Eksik kalan yeri en yeni yazılarla doldurmak listeyi doldurur ama başlığı yalan hâline getirir: "ilgili" sözcüğü ortak kategoriden başka bir şeye dayanmıyor.</summary>
    public static IReadOnlyList<BlogPostViewModel> Pick(BlogPostViewModel current, IEnumerable<BlogPostViewModel> candidates, int take = Count)
    {
        var currentCategories = current.Categories.Select(c => c.Id).ToHashSet();
        if (currentCategories.Count == 0)
            return [];

        return candidates
            .Where(p => p.Id != current.Id)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .Select(p => (Post: p, Shared: p.Categories.Count(c => currentCategories.Contains(c.Id))))
            .Where(x => x.Shared > 0)
            .OrderByDescending(x => x.Shared)
            .ThenByDescending(x => x.Post.CreatedAt)
            .ThenByDescending(x => x.Post.Id)
            .Take(take)
            .Select(x => x.Post)
            .ToList();
    }
}
