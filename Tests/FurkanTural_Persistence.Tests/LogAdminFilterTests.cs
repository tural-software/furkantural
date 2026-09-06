using System.Reflection;
using Dapper;
using FluentAssertions;
using FurkanTural_Persistence.Repositories.Concrete;

namespace FurkanTural_Persistence.Tests;

/// <summary>Kayıt defteri süzgecinin ürettiği WHERE parçası. Sorgu Dapper ile elle yazıldığı için sütun adları ve sınırlar derleyicinin göremediği dizelerde durur; burada okunan da o dizedir.<para>dateTo günün tamamını kapsasın diye bir gün eklenir. Takvimin son gününde bu toplama taşar ve istek 500'e döner — süzgeç kutusuna elle tarih yazan bir yönetici bunu tetikleyebilir. Üst sınır o durumda hiç konmaz; zaten hiçbir satırı elemezdi.</para></summary>
public class LogAdminFilterTests
{
    private static (string Where, DynamicParameters Parameters) Build(
        string? level = null, string? source = null, string? message = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var method = typeof(LogRepository).GetMethod("BuildAdminWhere", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((string, DynamicParameters))method.Invoke(null, [level, source, message, dateFrom, dateTo])!;
    }

    [Fact]
    public void Kaynak_susgeci_yeni_sutun_adini_kullanir()
    {
        var (where, parameters) = Build(source: "Auth-Login");

        where.Should().Contain("Source LIKE @Source").And.NotContain("Project",
            "sütun adı ham SQL'de düz metin; yeniden adlandırma burada da yapılmazsa sorgu çalışma zamanında patlar");
        parameters.Get<string>("Source").Should().Be("%Auth-Login%");
    }

    [Fact]
    public void Bitis_tarihi_gunun_tamamini_kapsar()
    {
        var (where, parameters) = Build(dateTo: new DateTime(2026, 9, 6));

        where.Should().Contain("Date < @DateTo");
        parameters.Get<DateTime>("DateTo").Should().Be(new DateTime(2026, 9, 7));
    }

    [Fact]
    public void Takvimin_son_gunu_ust_sinir_koymadan_gecer()
    {
        var (where, parameters) = Build(dateTo: new DateTime(9999, 12, 31));

        where.Should().NotContain("@DateTo",
            "bir gün eklemek DateTime taşmasına yol açardı; sınır hiç konmadığında sonuç aynı kalır");
        parameters.ParameterNames.Should().NotContain("DateTo");
    }

    [Fact]
    public void Bitis_tarihi_yoksa_sinir_da_yok()
        => Build().Where.Should().NotContain("@DateTo");

    [Fact]
    public void Baslangic_tarihi_oldugu_gibi_gecer()
    {
        var (where, parameters) = Build(dateFrom: new DateTime(2026, 9, 1, 13, 30, 0));

        where.Should().Contain("Date >= @DateFrom");
        parameters.Get<DateTime>("DateFrom").Should().Be(new DateTime(2026, 9, 1, 13, 30, 0),
            "başlangıç güne yuvarlanmaz; kırpılsaydı saat veren bir süzgeç sessizce genişlerdi");
    }
}
