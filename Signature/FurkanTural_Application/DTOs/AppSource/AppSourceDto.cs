namespace FurkanTural_Application.DTOs.AppSource;

/// <summary>Bir sunum projesinin liste görünümü. Code, giriş sırasında JWT'ye yazılan <c>app_source</c> claim'iyle aynı değerdir; gönderim tarafı şablonu bu kodla eşler.</summary>
public class AppSourceDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
