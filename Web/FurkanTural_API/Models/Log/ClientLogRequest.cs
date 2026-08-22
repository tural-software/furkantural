namespace FurkanTural_API.Models.Log;

/// <summary>Kayıtlı bir ön-yüz uygulamasının (app-token) gönderdiği istemci-tarafı log girdisi.</summary>
public class ClientLogRequest
{
    /// <summary>"Error" | "Warning" | "Information" (serbest gelen değerler normalize edilir).</summary>
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Path { get; set; }
    /// <summary>Tarayıcının IP'si; relay (ön-yüz sunucusu) doldurur. Yoksa bağlantı IP'si kullanılır.</summary>
    public string? IpAddress { get; set; }
}