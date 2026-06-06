namespace FurkanTural_Application.DTOs.Call;

/// <summary>
/// Cloudflare Realtime TURN "generate-ice-servers" yanıtı.
/// <c>iceServers</c> bir <b>dizi</b>dir (STUN + TURN girdileri).
/// Tarayıcı: <c>new RTCPeerConnection({ iceServers: dto.iceServers, iceTransportPolicy: 'relay' })</c>.
/// </summary>
public class TurnCredentialsDto
{
    public IceServerDto[] IceServers { get; set; } = [];
}

public class IceServerDto
{
    public string[] Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
