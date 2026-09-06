namespace FurkanTural_Admin.Models.Blog;

/// <summary>Bir bloğa atanmış etiketin hafif gösterimi (satır JSON'u + çip için). Rengi yoktur; etiket kategoriden görsel olarak ayrılmalıdır.</summary>
public sealed class BlogTagRef
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
