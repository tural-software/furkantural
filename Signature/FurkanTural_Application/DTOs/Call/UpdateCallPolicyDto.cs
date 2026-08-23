namespace FurkanTural_Application.DTOs.Call;

/// <summary>Politika güncelleme girdisi. Değerler doğrulanmaz, sessizce sınırlara çekilir: bit hızı 100–8000 kbps, genişlik 160–1920, yükseklik 120–1080, kare hızı 10–60. Aralık dışında değer gönderen istemci hata almaz, kaydedilen sayı gönderdiğinden farklı olur.</summary>
public class UpdateCallPolicyDto
{
    public bool BitrateLimitEnabled { get; set; }
    public int MaxVideoBitrateKbps { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int MaxFps { get; set; }
}
