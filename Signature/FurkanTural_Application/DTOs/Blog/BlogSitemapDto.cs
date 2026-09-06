namespace FurkanTural_Application.DTOs.Blog;

public class BlogSitemapDto
{
    public int Id { get; set; }

    /// <summary>Arşiv listesinin okunabilir olması için taşınır. Yazı gövdesi hâlâ çekilmez; başlık ayrı bir kolondur ve projeksiyona eklenmesi sorguya satır boyu dışında yük getirmez.</summary>
    public string? Title { get; set; }

    /// <summary>Kalıcı adres parçası. Site haritası ve arşiv bağlantıları bunu kullanır; kimlik adresine düşmek eski adresin dizinde kalmasına yol açardı.</summary>
    public string? Slug { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}