namespace FurkanTural_API.Models.Blog;

public class CreateBlogRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}

public class UpdateBlogRequest
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}