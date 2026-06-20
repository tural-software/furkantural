namespace FurkanTural_Application.DTOs.Category;

public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int? UpdatedBy { get; set; }
}
