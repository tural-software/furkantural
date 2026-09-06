namespace FurkanTural_API.Models.Blog;

public class CreateBlogRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? TagIds { get; set; }
}

public class UpdateBlogRequest
{
    public int Id { get; set; }
    public string? Title { get; set; }

    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse temizlenip tekilleştirilerek yazılır ve eski adres kırılır.</summary>
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? TagIds { get; set; }
}