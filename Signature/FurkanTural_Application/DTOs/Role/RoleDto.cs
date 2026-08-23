namespace FurkanTural_Application.DTOs.Role;

/// <summary>Yetki rolü. Name bir etiket değil anahtardır: token'a rol claim'i olarak bu metin yazılır ve API politikaları ("Admin", "User", "Subscriber", "Visitor") doğrudan onunla eşleşir; kayıt akışı da varsayılan rolü "User" adıyla arar. Dolayısıyla bir rolün adını değiştirmek yetkilendirmeyi hata vermeden bozar.</summary>
public class RoleDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
