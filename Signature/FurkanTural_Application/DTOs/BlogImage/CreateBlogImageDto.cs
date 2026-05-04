namespace FurkanTural_Application.DTOs.BlogImage;

public class CreateBlogImageDto
{
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsCover { get; set; }
    public int BlogId { get; set; }
    public int? CreatedBy { get; set; }
}