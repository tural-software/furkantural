namespace FurkanTural_Application.DTOs.Blog;

public class CreateBlogDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? CreatedBy { get; set; }
    public List<int>? CategoryIds { get; set; }
}