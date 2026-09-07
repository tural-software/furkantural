using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.DTOs.Tag;

namespace FurkanTural_Application.DTOs.Blog;

/// <summary>Yayındaki blog yazısı. Content ham Markdown'dır; API hiçbir yerde HTML'e çevirmez, dönüştürme sunum projelerinde yapılır. Categories ayrı bir sorgudan doldurulur, kategorisiz yazıda boş liste kalır.</summary>
public class BlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }

    /// <summary>Kalıcı adres parçası. Oluşturulurken başlıktan üretilir; başlık değişse de kendiliğinden değişmez, çünkü adresi değiştirmek dışarıdaki her bağlantıyı koparır.</summary>
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Yazının okunma sayısı. Sayfanın çizilmesiyle değil, tarayıcıdan gelen ayrı bir istekle artar; bu yüzden JavaScript çalıştırmayan gezginler sayıya girmez.</summary>
    public int ViewCount { get; set; }

    public List<CategoryDto> Categories { get; set; } = [];

    /// <summary>Yazının ikincil sınıflandırması. Kategoriler gibi ayrı bir sorgudan doldurulur; etiketsiz yazıda boş liste kalır.</summary>
    public List<TagDto> Tags { get; set; } = [];
}
