namespace FurkanTural_API.Models.Log;

/// <summary>Admin panelinin (AdminOnly JWT) sunucu-tarafı hata/uyarı loglarını sistem log tablosuna yazmak için gönderdiği girdi. Admin'in app-token'ı olmadığından ClientLog (AppClient) yerine bu AdminOnly uç nokta kullanılır.</summary>
public class CreateLogRequest
{
    /// <summary>"Error" | "Warning" | "Information" (serbest değerler normalize edilir).</summary>
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Path { get; set; }
    /// <summary>Olayın çıktığı bileşen ve işlem, tire ile: <c>Blog-Create-Post</c>. Uygulama adını sunucu damgalar (<c>FurkanTural_Admin</c>), yani gönderen kendini başka bir uygulama gibi gösteremez. Boş bırakılırsa kaynak yalnızca uygulama adından ibaret kalır.</summary>
    public string? Component { get; set; }
}
