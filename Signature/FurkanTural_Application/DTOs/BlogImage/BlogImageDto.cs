namespace FurkanTural_Application.DTOs.BlogImage;

public class BlogImageDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
}