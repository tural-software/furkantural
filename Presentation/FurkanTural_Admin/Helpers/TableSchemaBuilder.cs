using FurkanTural_Admin.Models.Schema;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Helpers;

/// <summary>Yirmi bir modülün tablo şeması sayfası tek view'ı paylaşır; modele dönüşen tek yer burasıdır. Modül adı, entity adı ve liste adresi <see cref="AdminModules"/> kaydından okunur — çağıran yalnızca kendi controller adını verir. Şema alınamazsa istisna fırlatılmaz: sayfa hata kutusuyla çizilir, çünkü şema görüntüleyememek bir yönetim işlemini engellemez.</summary>
public static class TableSchemaBuilder
{
    public static async Task<TableSchemaViewModel> BuildAsync(
        ISchemaApiClient schemaApiClient,
        IUrlHelper url,
        string controller,
        string token,
        CancellationToken cancellationToken)
    {
        var module = AdminModules.ByController(controller);
        if (module is null)
        {
            return new TableSchemaViewModel
            {
                ModuleTitle = controller,
                ModuleUrl = url.Action("Index", controller) ?? "/",
                Schema = null,
                ErrorMessage = "Bu modül gezinme kaydında tanımlı değil."
            };
        }

        var schema = await schemaApiClient.GetAsync(module.Entity, token, cancellationToken);

        return new TableSchemaViewModel
        {
            ModuleTitle = module.Title,
            ModuleUrl = url.Action("Index", module.Controller) ?? "/",
            Schema = schema,
            ErrorMessage = schema is null ? "Tablo şeması alınamadı; API'ye erişilemiyor olabilir." : null
        };
    }
}
