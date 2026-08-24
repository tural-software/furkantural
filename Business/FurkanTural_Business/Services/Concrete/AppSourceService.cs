using FurkanTural_Application.DTOs.AppSource;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Okuma küresel süzgeçten geçer, dolayısıyla pasife alınmış bir proje seçenek olarak sunulmaz. Sıralama tohumdaki SortOrder'a göredir; ad alfabetik değildir çünkü listenin okunma sırası projelerin önem sırasıdır.</summary>
public class AppSourceService(IUnitOfWork unitOfWork) : IAppSourceService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<IEnumerable<AppSourceDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.AppSources.GetAllAsync(cancellationToken);

        return Result<IEnumerable<AppSourceDto>>.Ok(entities
            .OrderBy(e => e.SortOrder)
            .Select(e => new AppSourceDto
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                SortOrder = e.SortOrder
            }));
    }
}
