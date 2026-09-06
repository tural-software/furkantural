using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Etiket yönetimi. Sözleşme <see cref="ICategoryService"/> ile aynı beşliyi tekrarlar; ikisi arasındaki fark davranışta değil ölçektedir.<para>Bir farkı vardır ve bilinçlidir: ad tekildir. Etiket sayıca kalabalıklaşacağı için aynı adın ikinci kez açılması kaçınılmazdır ve iki "EF Core" etiketi listeyi ikiye bölerdi. Kural veri tabanında durur, servis yalnızca çakışmayı okunur bir mesaja çevirir.</para><para><see cref="GetPopularAsync"/> etiket bulutu içindir: yazısı olmayan etiketler dışarıda kalır, çünkü boş bir etiket sayfası okuru hiçbir yere götürmez.</para></summary>
public interface ITagService : IService<TagDto, CreateTagDto, UpdateTagDto>, IBulkService
{
    Task<Result<TagDto>> GetBySlugAsync(string? slug, CancellationToken cancellationToken = default);

    /// <summary>Yazısı olan etiketler, yazı sayısına göre azalan. Sayı eşitse ada göre sıralanır; aksi hâlde bulutun sırası okuma sırasına göre değişirdi.</summary>
    Task<Result<IEnumerable<AdminTagDto>>> GetPopularAsync(int take, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<AdminTagDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminTagDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminTagDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminTagDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminTagDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
}
