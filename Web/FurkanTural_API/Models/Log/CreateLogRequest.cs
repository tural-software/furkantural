namespace FurkanTural_API.Models.Log;

/// <summary>
/// Admin panelinin (AdminOnly JWT) sunucu-tarafı hata/uyarı loglarını sistem log tablosuna
/// yazmak için gönderdiği girdi. Admin'in app-token'ı olmadığından ClientLog (AppClient) yerine
/// bu AdminOnly uç nokta kullanılır.
/// </summary>
public class CreateLogRequest
{
    /// <summary>"Error" | "Warning" | "Information" (serbest değerler normalize edilir).</summary>
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Path { get; set; }
    /// <summary>Kaynak uygulama; boşsa "FurkanTural_Admin" damgalanır.</summary>
    public string? Project { get; set; }
}