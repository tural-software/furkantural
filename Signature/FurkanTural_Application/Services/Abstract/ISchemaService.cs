using FurkanTural_Application.DTOs.Schema;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Yönetim panelinin tablo şeması sayfasını besler. Salt okunurdur; yazma, güncelleme veya silme ucu yoktur.</summary>
public interface ISchemaService
{
    Result<TableSchemaDto> Get(string? entity);
}
