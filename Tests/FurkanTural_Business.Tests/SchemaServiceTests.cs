using FluentAssertions;
using FurkanTural_Application.DTOs.Schema;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Constants;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Beyaz liste bu ucun güvenlik sınırıdır: liste dışı bir ad geldiğinde EF modeline hiç bakılmamalıdır. Testler yalnızca dönen durumu değil, okuyucunun çağrılmadığını da doğrular — sınır aşılırsa uç modeldeki her tipi yoklamak için kullanılabilir hâle gelir.</summary>
public class SchemaServiceTests
{
    private static TableSchemaDto OrnekSema() => new()
    {
        Entity = SchemaEntityDefinitions.Blog,
        TableName = "Blogs",
        Columns = [new TableColumnDto { Name = "Id", ColumnType = "int", IsPrimaryKey = true, IsIdentity = true }]
    };

    [Theory]
    [InlineData("DbContext")]
    [InlineData("AccountActivation")]
    [InlineData("AppSource")]
    [InlineData("BlogCategory")]
    [InlineData("PushSubscription")]
    [InlineData("System.String")]
    [InlineData("blog")]
    [InlineData("")]
    [InlineData(null)]
    public void Beyaz_liste_disindaki_ad_icin_okuyucuya_hic_gidilmez(string? entity)
    {
        var reader = new Mock<ISchemaMetadataReader>(MockBehavior.Strict);
        var service = new SchemaService(reader.Object);

        var result = service.Get(entity);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        reader.Verify(r => r.Read(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Beyaz_listedeki_ad_okuyucuya_gecirilir()
    {
        var reader = new Mock<ISchemaMetadataReader>();
        reader.Setup(r => r.Read(SchemaEntityDefinitions.Blog)).Returns(OrnekSema());

        var result = new SchemaService(reader.Object).Get(SchemaEntityDefinitions.Blog);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data!.TableName.Should().Be("Blogs");
        reader.Verify(r => r.Read(SchemaEntityDefinitions.Blog), Times.Once);
    }

    [Fact]
    public void Beyaz_listede_olup_modelde_bulunmayan_ad_da_404_doner()
    {
        var reader = new Mock<ISchemaMetadataReader>();
        reader.Setup(r => r.Read(It.IsAny<string>())).Returns((TableSchemaDto?)null);

        var result = new SchemaService(reader.Object).Get(SchemaEntityDefinitions.Status);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Basarisiz_yanit_disariya_ayrinti_sizdirmaz()
    {
        var reader = new Mock<ISchemaMetadataReader>(MockBehavior.Strict);

        var result = new SchemaService(reader.Object).Get("GizliTip");

        result.Errors.Should().ContainSingle().Which.Should().Be("Tablo bulunamadı.");
        result.Errors.Should().NotContain(e => e.Contains("GizliTip"));
    }

    [Fact]
    public void Beyaz_liste_yirmi_bir_modulu_kapsar()
    {
        SchemaEntityDefinitions.All.Should().HaveCount(21);
    }

    [Theory]
    [InlineData("Blog")]
    [InlineData("MailTemplate")]
    [InlineData("UserFriend")]
    [InlineData("Log")]
    public void Panelin_yonettigi_moduller_beyaz_listededir(string entity)
    {
        SchemaEntityDefinitions.IsAllowed(entity).Should().BeTrue();
    }
}
