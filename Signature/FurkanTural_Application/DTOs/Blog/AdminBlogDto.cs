using FurkanTural_Application.DTOs.Category;

namespace FurkanTural_Application.DTOs.Blog;

public class AdminBlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }

    /// <summary>Kalıcı adres parçası. Oluşturulurken başlıktan üretilir; başlık değişse de kendiliğinden değişmez, çünkü adresi değiştirmek dışarıdaki her bağlantıyı koparır.</summary>
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public List<CategoryDto> Categories { get; set; } = [];
}