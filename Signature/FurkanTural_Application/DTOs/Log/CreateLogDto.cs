namespace FurkanTural_Application.DTOs.Log;

/// <summary>Kayıt defterine satır ekleme. Yalnızca Message zorunludur; Date dahil hiçbir alan sunucuda doldurulmaz, dolayısıyla Date gönderilmezse satır 0001-01-01 ile yazılır ve sıralama o alana göre yapıldığı için listede en dibe düşer. Project ile Level'ın sabit listesi yoktur, serbest metindir.</summary>
public class CreateLogDto
{
    public string? Project { get; set; }
    public DateTime Date { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? Path { get; set; }
}
