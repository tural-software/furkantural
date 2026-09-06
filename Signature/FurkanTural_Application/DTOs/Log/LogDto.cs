namespace FurkanTural_Application.DTOs.Log;

/// <summary>Kayıt defteri satırı. Date, satırın oluşturulma zamanı değil olayın kendi zaman damgasıdır — çağıran gönderir — ve listeler bu alana göre sıralanır, satırın CreatedAt'ine göre değil.</summary>
public class LogDto
{
    public int Id { get; set; }
    public string? Source { get; set; }
    public DateTime Date { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string? Path { get; set; }
}
