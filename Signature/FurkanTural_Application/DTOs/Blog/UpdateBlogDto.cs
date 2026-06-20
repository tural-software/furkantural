namespace FurkanTural_Application.DTOs.Blog;

public class UpdateBlogDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? UpdatedBy { get; set; }

    /// <summary>Bloğun yeni kategori kümesi (null ise kategoriler değiştirilmez).</summary>
    public List<int>? CategoryIds { get; set; }
}
