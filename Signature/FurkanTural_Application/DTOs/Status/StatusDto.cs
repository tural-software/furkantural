namespace FurkanTural_Application.DTOs.Status;

/// <summary>Uygulama genelindeki durum sözlüğünün bir satırı. Gerçek anahtar Id değil Group ve Code ikilisidir; benzersizlik de o ikili üzerinden korunur ve kod hiçbir yerde Id sabiti taşımaz, çünkü aynı statünün Id'si ortamdan ortama değişir. Kanonik Group/Code değerleri <see cref="FurkanTural_Domain.Constants.StatusDefinitions"/> içinde tutulur.</summary>
public class StatusDto
{
    public int Id { get; set; }
    public string? Group { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}
