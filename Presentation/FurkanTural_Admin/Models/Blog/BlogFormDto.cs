namespace FurkanTural_Admin.Models.Blog;

public sealed class BlogFormDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<int>? CategoryIds { get; set; }
}