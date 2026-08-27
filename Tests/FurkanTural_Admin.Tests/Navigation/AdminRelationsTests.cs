using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.BlogImage;
using FurkanTural_Admin.Models.MusicImage;
using FurkanTural_Admin.Models.ProjectImage;
using FurkanTural_Admin.Models.User;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Admin.Tests.Navigation;

/// <summary>Detay çekmecesindeki "İlişkili" sekmesi. İlişkiler yalnızca listede zaten geçiş düğmesi bulunan çiftlerdir; bu testler bağlantının gerçek bir modüle gittiğini ve sayının oturum olmadan sızmadığını doğrular.</summary>
public class AdminRelationsTests
{
    private static RelatedController BuildSut(
        string? token,
        IReadOnlyList<BlogImageAdminDto>? blogImages = null,
        IReadOnlyList<UserAdminDto>? users = null)
    {
        var blogImageClient = new Mock<IBlogImageApiClient>(MockBehavior.Loose);
        blogImageClient
            .Setup(c => c.GetAllForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blogImages ?? []);

        var musicImageClient = new Mock<IMusicImageApiClient>(MockBehavior.Loose);
        musicImageClient
            .Setup(c => c.GetAllForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MusicImageAdminDto>());

        var projectImageClient = new Mock<IProjectImageApiClient>(MockBehavior.Loose);
        projectImageClient
            .Setup(c => c.GetAllForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProjectImageAdminDto>());

        var userClient = new Mock<IUserApiClient>(MockBehavior.Loose);
        userClient
            .Setup(c => c.GetAllForAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users ?? []);

        return new RelatedController(
            blogImageClient.Object, musicImageClient.Object, projectImageClient.Object, userClient.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext(token),
            Url = ControllerTestHelper.BuildUrlHelper("/BlogImage?blogId=7")
        };
    }

    [Fact]
    public async Task Token_yoksa_sayilar_sizmaz()
    {
        var sut = BuildSut(null);

        var result = await sut.Counts("Blog", 7, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Iliskisi_olmayan_modul_bos_liste_doner()
    {
        var sut = BuildSut("token");

        var result = await sut.Counts("Skill", 1, CancellationToken.None);

        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task Alt_kayitlar_ebeveyne_gore_sayilir()
    {
        var users = new List<UserAdminDto>
        {
            new() { Id = 1, RoleId = 1 },
            new() { Id = 2, RoleId = 1 },
            new() { Id = 3, RoleId = 2 },
        };

        var sut = BuildSut("token", users: users);

        var result = await sut.Counts("Role", 1, CancellationToken.None) as JsonResult;

        result.Should().NotBeNull();
        var payload = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        payload.Should().Contain("\"count\":2", "rol 1'e bağlı iki kullanıcı var");
    }

    [Fact]
    public void Her_iliskinin_iki_ucu_da_kayitli_bir_modul()
    {
        foreach (var entity in new[] { "Blog", "Music", "Project", "Role" })
        {
            var relations = AdminRelations.For(entity);
            relations.Should().NotBeEmpty($"{entity} için ilişki tanımlı olmalı");

            AdminModules.ByController(entity).Should().NotBeNull($"{entity} kayıtta olmalı");

            foreach (var relation in relations)
            {
                AdminModules.ByController(relation.ChildController)
                    .Should().NotBeNull($"{relation.ChildController} kayıtta olmalı — bağlantı 404'e gitmemeli");
            }
        }
    }

    [Fact]
    public void Suzgec_anahtari_alt_modulun_gercek_parametresidir()
    {
        var sapan = new List<string>();

        foreach (var entity in new[] { "Blog", "Music", "Project", "Role" })
        {
            foreach (var relation in AdminRelations.For(entity))
            {
                var controller = typeof(AdminModules).Assembly
                    .GetTypes()
                    .SingleOrDefault(t => t.Name == $"{relation.ChildController}Controller");

                if (controller is null)
                {
                    sapan.Add($"{relation.ChildController}Controller bulunamadı");
                    continue;
                }

                var index = controller.GetMethod("Index");
                var parametreler = index?.GetParameters().Select(p => p.Name).ToArray() ?? [];

                if (!parametreler.Contains(relation.FilterKey))
                    sapan.Add($"{entity} → {relation.ChildController}: \"{relation.FilterKey}\" "
                            + $"bir Index parametresi değil (var olanlar: {string.Join(", ", parametreler)})");
            }
        }

        sapan.Should().BeEmpty(
            "süzgeç anahtarı alt listenin tanımadığı bir ad olursa bağlantı sessizce süzmez, "
          + "kullanıcı tüm kayıtları görür ve hata da almaz");
    }

    [Fact]
    public void Kayitta_olmayan_modul_icin_iliski_yoktur()
    {
        AdminRelations.For("Skill").Should().BeEmpty();
        AdminRelations.For(null).Should().BeEmpty();
        AdminRelations.For("").Should().BeEmpty();
    }
}
