using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Navigation;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FurkanTural_Admin.Controllers;

/// <summary>Detay çekmecesindeki "İlişkili" sekmesi buradan beslenir: bir kaydın alt kayıt sayısı ve süzülmüş listeye giden adres. Sayım Admin tarafında yapılır; API sözleşmesi değişmez.</summary>
public class RelatedController(
    IBlogImageApiClient blogImageApiClient,
    IMusicImageApiClient musicImageApiClient,
    IProjectImageApiClient projectImageApiClient,
    IUserApiClient userApiClient) : Controller
{
    private readonly IBlogImageApiClient _blogImageApiClient = blogImageApiClient;
    private readonly IMusicImageApiClient _musicImageApiClient = musicImageApiClient;
    private readonly IProjectImageApiClient _projectImageApiClient = projectImageApiClient;
    private readonly IUserApiClient _userApiClient = userApiClient;

    [HttpGet]
    public async Task<IActionResult> Counts(string? entity, int id, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var relations = AdminRelations.For(entity);
        if (relations.Count == 0)
            return Json(new { items = Array.Empty<object>() });

        var items = new List<object>(relations.Count);

        foreach (var relation in relations)
        {
            var module = AdminModules.ByController(relation.ChildController);
            if (module is null) continue;

            items.Add(new
            {
                title = module.Title,
                icon = module.Slug,
                unit = module.CountUnitLabel,
                count = await CountAsync(relation, id, token, cancellationToken),
                url = Url.Action("Index", relation.ChildController, BuildFilter(relation.FilterKey, id))
            });
        }

        return Json(new { items });
    }

    private static RouteValueDictionary BuildFilter(string key, int id) =>
        new() { [key] = id };

    private async Task<int> CountAsync(AdminRelation relation, int id, string token, CancellationToken cancellationToken) =>
        relation.ChildController switch
        {
            "BlogImage" => (await _blogImageApiClient.GetAllForAdminAsync(token, cancellationToken))
                .Count(x => x.BlogId == id),
            "MusicImage" => (await _musicImageApiClient.GetAllForAdminAsync(token, cancellationToken))
                .Count(x => x.MusicId == id),
            "ProjectImage" => (await _projectImageApiClient.GetAllForAdminAsync(token, cancellationToken))
                .Count(x => x.ProjectId == id),
            "User" => (await _userApiClient.GetAllForAdminAsync(token, cancellationToken))
                .Count(x => x.RoleId == id),
            _ => 0
        };
}
