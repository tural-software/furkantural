using System.Reflection;
using FluentAssertions;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FurkanTural_API.Tests;

/// <summary>Yönetici rotaları silinmiş ve pasif satırları da döndürür; bu yüzden "admin" ile başlayan her rota ya sınıfında ya da metodunda AdminOnly taşımak zorundadır. Test derlenmiş API'yi yansımayla tarar, dolayısıyla yeni eklenen her uç kendiliğinden kapsama girer — unutulan bir öznitelik burada yakalanır, üretimde değil.</summary>
public class AdminRoutePolicyTests
{
    private static IEnumerable<(Type Controller, MethodInfo Action, string Template)> AdminActions()
    {
        var controllers = typeof(BaseApiController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controller in controllers)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    var template = http.Template ?? string.Empty;
                    if (template == "admin" || template.StartsWith("admin/", StringComparison.Ordinal))
                        yield return (controller, action, template);
                }
            }
        }
    }

    private static bool IsAdminOnly(MemberInfo member)
        => member.GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Policy == "AdminOnly");

    [Fact]
    public void Taramanin_kendisi_bos_donmez()
    {
        AdminActions().Count().Should().BeGreaterThan(20,
            "yönetici rotası bulunamıyorsa bu test hiçbir şeyi doğrulamıyor demektir");
    }

    [Fact]
    public void Her_admin_rotasi_AdminOnly_tasir()
    {
        var acik = AdminActions()
            .Where(x => !IsAdminOnly(x.Action) && !IsAdminOnly(x.Controller))
            .Select(x => $"{x.Controller.Name}.{x.Action.Name} [{x.Template}]")
            .ToList();

        acik.Should().BeEmpty(
            "bu rotalar süzgeçsiz okur; AdminOnly olmayan bir tanesi silinmiş ve pasif kayıtları " +
            "yönetici olmayana açar:" + Environment.NewLine + string.Join(Environment.NewLine, acik));
    }

    [Fact]
    public void Sayfali_admin_rotalari_sayfa_boyu_alir()
    {
        var sayfasiz = AdminActions()
            .Where(x => x.Template.EndsWith("/paged", StringComparison.Ordinal))
            .Where(x => !x.Action.GetParameters().Any(p => p.Name == "pageSize"))
            .Select(x => $"{x.Controller.Name}.{x.Action.Name}")
            .ToList();

        sayfasiz.Should().BeEmpty("adı 'paged' olan bir rota sayfa boyu almıyorsa sayfalamıyordur");
    }
}
