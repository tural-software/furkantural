using System.Reflection;
using FluentAssertions;
using FurkanTural_Admin.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Gezinme kaydı artık modül adının, grubunun ve controller'ının tek kaynağı. Bu testler kaydın gerçekten var olan controller'lara ve ikonlara bağlandığını doğrular; sessizce boş çizilen bir satır ya da 404'e giden bir bağlantı buradan sızmaz.</summary>
public class AdminModulesTests
{
    private static readonly Type[] AdminControllers =
        [.. typeof(FurkanTural_Admin.Controllers.DashboardController).Assembly
              .GetTypes()
              .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)];

    [Fact]
    public void Yirmi_bir_modul_kayitlidir()
    {
        AdminModules.All.Should().HaveCount(21);
    }

    [Fact]
    public void Slug_entity_ve_controller_adlari_benzersizdir()
    {
        AdminModules.All.Select(m => m.Slug).Should().OnlyHaveUniqueItems();
        AdminModules.All.Select(m => m.Entity).Should().OnlyHaveUniqueItems();
        AdminModules.All.Select(m => m.Controller).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Her_modulun_controller_i_gercekten_vardir()
    {
        var eksik = AdminModules.All
            .Where(m => AdminControllers.All(t => t.Name != m.Controller + "Controller"))
            .Select(m => m.Controller)
            .ToList();

        eksik.Should().BeEmpty("kayıttaki her controller adı gerçek bir controller'a karşılık gelmeli");
    }

    [Fact]
    public void Her_modulun_Index_ve_TableDetail_action_i_vardir()
    {
        var eksik = new List<string>();

        foreach (var module in AdminModules.All)
        {
            var type = AdminControllers.Single(t => t.Name == module.Controller + "Controller");
            foreach (var action in new[] { "Index", "TableDetail" })
            {
                if (type.GetMethod(action, BindingFlags.Public | BindingFlags.Instance) is null)
                    eksik.Add($"{type.Name}.{action}");
            }
        }

        eksik.Should().BeEmpty("modül seçici ve kırıntı yolu bu action'lara bağlanıyor");
    }

    [Fact]
    public void Her_modulun_slug_u_icin_ikon_vardir()
    {
        var eksik = AdminModules.All
            .Where(m => string.IsNullOrEmpty(IconLibrary.Render(m.Slug)))
            .Select(m => m.Slug)
            .ToList();

        eksik.Should().BeEmpty("ikon bulunamazsa satır sessizce boş çizilir");
    }

    [Fact]
    public void Gruplama_tum_modulleri_bir_kez_ve_bildirilen_sirayla_dondurur()
    {
        var gruplar = AdminModules.Grouped();

        gruplar.Select(g => g.Key).Should().Equal(
            AdminModules.GroupContent,
            AdminModules.GroupProfile,
            AdminModules.GroupCommunity,
            AdminModules.GroupContact,
            AdminModules.GroupSystem);

        gruplar.SelectMany(g => g).Should().BeEquivalentTo(AdminModules.All);
    }

    [Fact]
    public void Grup_icindeki_sira_bildirim_sirasini_korur()
    {
        var icerik = AdminModules.Grouped().First(g => g.Key == AdminModules.GroupContent);

        icerik.Select(m => m.Slug).Should().Equal(
            "blogs", "blog-images", "categories", "projects", "project-images", "music", "music-images");
    }

    [Theory]
    [InlineData("Blog", "blogs")]
    [InlineData("blog", "blogs")]
    [InlineData("MAILTEMPLATE", "mail-template")]
    [InlineData("UserFriend", "friends")]
    public void ByController_buyuk_kucuk_harf_gozetmez(string controller, string beklenenSlug)
    {
        AdminModules.ByController(controller)!.Slug.Should().Be(beklenenSlug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Auth")]
    [InlineData("Dashboard")]
    public void Kayitta_olmayan_controller_icin_null_doner(string? controller)
    {
        AdminModules.ByController(controller).Should().BeNull();
    }

    [Fact]
    public void Her_modulun_metinleri_doludur()
    {
        foreach (var m in AdminModules.All)
        {
            m.Title.Should().NotBeNullOrWhiteSpace();
            m.Description.Should().NotBeNullOrWhiteSpace();
            m.CountUnitLabel.Should().NotBeNullOrWhiteSpace();
            m.Actions.Should().NotBeEmpty();
        }
    }
}
