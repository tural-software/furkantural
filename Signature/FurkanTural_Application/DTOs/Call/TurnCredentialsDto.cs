namespace FurkanTural_Application.DTOs.Call;

/// <summary>
/// Cloudflare Realtime'dan alınan ICE sunucu listesi. Kimlik bilgileri kısa ömürlüdür (24 saat) ve her
/// istekte yeniden üretilir, saklanmaya uygun değildir.
/// </summary>
public class TurnCredentialsDto
{
    public IceServerDto[] IceServers { get; set; } = [];
}

/// <summary>
/// Tarayıcının RTCIceServer nesnesiyle aynı şekli taşır, doğrudan RTCPeerConnection'a verilir. STUN
/// girdilerinde Username ve Credential boş kalır, yalnızca TURN girdileri doldurulur.
/// </summary>
public class IceServerDto
{
    public string[] Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Credential { get; set; }
}