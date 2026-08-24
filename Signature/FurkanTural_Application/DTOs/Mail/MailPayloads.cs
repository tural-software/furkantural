using FurkanTural_Domain.Constants;

namespace FurkanTural_Application.DTOs.Mail;

/// <summary>Bir posta türünün şablonunda kullanılabilecek yer tutucuların tek kaynağı. Şablondaki <c>{{Ad}}</c> ifadeleri buradaki özellik adlarıyla birebir eşleşir, dolayısıyla "hangi alanlar var" sorusunun cevabı elle tutulan bir listeden değil tipin kendisinden gelir ve ikisi birbirinden ayrışamaz.<para>Bütün alanlar metindir. Biçimlendirme — tarih düzeni, sayı, yerel ayar — postayı hazırlayan servisin işidir; şablon motoru yalnızca değiştirme yapar. Böylece aynı DTO farklı biçimlerle doldurulabilir ve biçim kararı çağıranın gözünün önünde kalır.</para></summary>
public static class MailPayloads
{
    /// <summary>Tür kodundan o türün gövde tipine. Yönetim paneli kullanılabilir yer tutucuları buradan üretir; listede olmayan bir kod panelden eklenmiş demektir ve karşılığında gönderen bir kod yolu yoktur.</summary>
    public static readonly IReadOnlyDictionary<string, Type> ByTypeCode = new Dictionary<string, Type>
    {
        [MailTemplateDefinitions.ContactOwner] = typeof(ContactOwnerMailDto),
        [MailTemplateDefinitions.ContactUser] = typeof(ContactUserMailDto),
        [MailTemplateDefinitions.AccountActivation] = typeof(AccountActivationMailDto)
    };

    public static IReadOnlyList<string> PlaceholdersOf(string? typeCode)
        => typeCode is not null && ByTypeCode.TryGetValue(typeCode, out var type)
            ? [.. type.GetProperties().Select(p => p.Name)]
            : [];
}

/// <summary>İletişim formu doldurulduğunda site sahibine düşen bildirimin gövdesi.</summary>
public class ContactOwnerMailDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public string? CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public string? FormPageUrl { get; set; }
}

/// <summary>İletişim formunu dolduran kişiye giden alındı yanıtının gövdesi.</summary>
public class ContactUserMailDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public string? CreatedAt { get; set; }
    public string? CurrentYear { get; set; }
    public string? ContactEmail { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? InstagramUrl { get; set; }
}

/// <summary>Pasife alınmış bir hesabı yeniden açan doğrulama postasının gövdesi. ActivationUrl bir kimlik bilgisi taşır: jetonun düz hâli yalnızca bu bağlantının içinde bulunur, hiçbir kayda yazılmaz.</summary>
public class AccountActivationMailDto
{
    public string? DisplayName { get; set; }
    public string? ActivationUrl { get; set; }
    public string? ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public string? ContactEmail { get; set; }
    public string? CurrentYear { get; set; }
}
