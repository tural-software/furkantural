namespace FurkanTural_Application.DTOs.Call;

/// <summary>Tarayıcının WebRTC bağlantısını kurmak için ihtiyaç duyduğu her şey tek yanıtta. ICE sunucularının kaynağı <c>Calls:Ice:Mode</c> ile seçilir: "Cloudflare" kısa ömürlü TURN kimlik bilgisi ürettirir, "Static" ise yapılandırmadaki STUN listesini token'sız döndürür. RelayOnly bundan bağımsız okunur (<c>Calls:Ice:RelayOnly</c>, varsayılan true) ve istemcide iceTransportPolicy'yi relay'e sabitler, yani taraf IP'leri karşıya geçmez — Static kipte relay adayı üretilmediği için ikisinin birlikte açık kalması bağlantıyı kurulamaz hâle getirir.</summary>
public class CallConfigDto
{
    public IceServerDto[] IceServers { get; set; } = [];
    public VideoPolicyDto VideoPolicy { get; set; } = new();
    public bool RelayOnly { get; set; } = true;
}
