using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Görüntülü arama kalite politikası. Tabloda tek bir satır tutulur ve herkese uygulanır; GetEffectiveForUserAsync'in userId parametresi ileride kullanıcı bazlı politika için açılmış dikiştir, bugün sonucu etkilemez. Satır bulunamazsa hata değil koda gömülü varsayılan politika döner, yani çağıran her zaman kullanılabilir bir değer alır. UpdateAsync gelen değerleri sabit alt ve üst sınırlara kırpar.</summary>
public interface ICallPolicyService
{
    Task<VideoPolicyDto> GetEffectiveForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<AdminCallPolicyDto>> GetForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminCallPolicyDto>> UpdateAsync(UpdateCallPolicyDto dto, int? updatedBy, CancellationToken cancellationToken = default);
}
