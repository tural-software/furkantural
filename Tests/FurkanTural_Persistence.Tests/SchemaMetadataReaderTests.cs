using FluentAssertions;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Şema okuyucusunun kaynağı EF modelinin kendisidir. Testler bağlantı açmaz — model kurulumu veri tabanı gerektirmez, dolayısıyla okunan değerler yapılandırmanın ne söylediğini gösterir.</summary>
public class SchemaMetadataReaderTests
{
    private static SchemaMetadataReader Reader()
    {
        var options = new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options;

        return new SchemaMetadataReader(new FurkanTuralDbContext(options));
    }

    [Fact]
    public void Blog_Title_kolonu_yapilandirmadaki_500_uzunlugu_ile_okunur()
    {
        var schema = Reader().Read("Blog");

        schema.Should().NotBeNull();
        schema!.TableName.Should().Be("Blogs");

        var title = schema.Columns.Single(c => c.Name == "Title");

        title.MaxLength.Should().Be(500);
        title.ColumnType.Should().Be("nvarchar(500)");
        title.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Birincil_anahtar_ve_identity_dogru_isaretlenir()
    {
        var schema = Reader().Read("Blog");

        var id = schema!.Columns.Single(c => c.Name == "Id");

        id.IsPrimaryKey.Should().BeTrue();
        id.IsIdentity.Should().BeTrue();
        id.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Denetim_alanlari_her_entity_de_bulunur()
    {
        var schema = Reader().Read("Blog");

        schema!.Columns.Select(c => c.Name).Should().Contain(
            ["CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsActive", "IsDeleted", "DeletedAt"]);
    }

    [Fact]
    public void IsActive_ve_IsDeleted_varsayilanlari_semaya_yazilir()
    {
        var schema = Reader().Read("Blog");

        schema!.Columns.Single(c => c.Name == "IsActive").DefaultValue.Should().NotBeNull();
        schema.Columns.Single(c => c.Name == "IsDeleted").DefaultValue.Should().NotBeNull();
    }

    [Fact]
    public void Yapilandirilmamis_varsayilan_uydurulmaz()
    {
        var schema = Reader().Read("Blog");

        schema!.Columns.Single(c => c.Name == "Id").DefaultValue.Should().BeNull();
        schema.Columns.Single(c => c.Name == "CreatedAt").DefaultValue.Should().BeNull();
        schema.Columns.Single(c => c.Name == "Title").DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Varsayilani_olan_kolon_identity_sayilmaz()
    {
        var schema = Reader().Read("Blog");

        schema!.Columns.Single(c => c.Name == "IsActive").IsIdentity.Should().BeFalse();
        schema.Columns.Single(c => c.Name == "IsDeleted").IsIdentity.Should().BeFalse();
        schema.Columns.Single(c => c.Name == "Id").IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Modelde_olmayan_entity_icin_null_doner()
    {
        Reader().Read("BoyleBirSeyYok").Should().BeNull();
    }

    [Theory]
    [InlineData("Blog", "Blogs")]
    [InlineData("User", "Users")]
    [InlineData("Log", "Logs")]
    [InlineData("Report", "Reports")]
    public void Beyaz_listedeki_entity_ler_modelde_karsilik_bulur(string entity, string tableName)
    {
        var schema = Reader().Read(entity);

        schema.Should().NotBeNull($"'{entity}' beyaz listede duruyor; modelde karşılığı olmalı");
        schema!.TableName.Should().Be(tableName);
        schema.Columns.Should().NotBeEmpty();
    }
}
