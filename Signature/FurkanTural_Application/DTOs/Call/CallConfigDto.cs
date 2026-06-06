namespace FurkanTural_Application.DTOs.Call;

/// <summary>Arama başında istemciye verilen birleşik yapılandırma: ICE sunucuları + video politikası.</summary>
public class CallConfigDto
{
    public IceServerDto[] IceServers { get; set; } = [];
    public VideoPolicyDto VideoPolicy { get; set; } = new();
    /// <summary>true → istemci <c>iceTransportPolicy:'relay'</c> (IP gizli, TURN şart); false → <c>'all'</c> (P2P'ye izin).</summary>
    public bool RelayOnly { get; set; } = true;
}
