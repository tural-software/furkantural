using FurkanTural_Application.DTOs.AppSource;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Sunum projelerinin listesi. Yazma ucu bilerek yoktur: bir satırın karşılığı ancak o adı taşıyan gerçek bir ön-yüz ve ona app-token veren bir yapılandırma varsa oluşur, dolayısıyla panelden proje "eklemek" yalnızca boş bir seçenek doğururdu. Liste değişecekse önce çözüme yeni bir sunum projesi girer, sonra tohum genişletilir.</summary>
public interface IAppSourceService
{
    Task<Result<IEnumerable<AppSourceDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}
