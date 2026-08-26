using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

/// <summary>Yönetim panelinin tablo şeması sayfasını besler. Uç veri tabanı şemasını açtığı için yalnızca yöneticiye kapalıdır; salt okunurdur ve hiçbir satır döndürmez.<para><see cref="BaseApiController"/> tercih edildi: kullanıcı claim'lerine ihtiyaç duymadığından <c>JwtBaseController</c> gerekmez — aynı gerekçeyle <c>LogController</c> ve <c>ConfigController</c> de bu tabanı kullanır.</para></summary>
[Authorize(Policy = "AdminOnly")]
[ApiVersion("1.0")]
public class SchemaController(ISchemaService schemaService) : BaseApiController
{
    private readonly ISchemaService _schemaService = schemaService;

    /// <summary>Bir entity'nin tablo şemasını getir</summary>
    [HttpGet("{entity}")]
    public IActionResult GetByEntity(string entity)
        => ToActionResult(_schemaService.Get(entity));
}
