namespace FurkanTural_Application.DTOs.Log;

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