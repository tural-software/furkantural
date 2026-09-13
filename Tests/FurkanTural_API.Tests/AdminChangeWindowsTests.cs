using FluentAssertions;
using FurkanTural_API.Realtime;
using FurkanTural_Application.DTOs.Common;

namespace FurkanTural_API.Tests;

public class AdminChangeWindowsTests
{
    private static readonly DateTime T0 = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(AdminListKinds.Comment, 1)]
    [InlineData(AdminListKinds.Contact, 1)]
    [InlineData(AdminListKinds.Report, 1)]
    [InlineData(AdminListKinds.User, 2)]
    [InlineData(AdminListKinds.Friend, 2)]
    [InlineData(AdminListKinds.Subscriber, 2)]
    [InlineData(AdminListKinds.Newsletter, 5)]
    [InlineData(AdminListKinds.Message, 10)]
    [InlineData(AdminListKinds.Call, 10)]
    public void Pencere_suresi_turun_yazma_sikligina_gore_secilir(string kind, int seconds)
    {
        AdminChangeWindows.WindowFor(kind).Should().Be(TimeSpan.FromSeconds(seconds),
            "iş kuyrukları neredeyse anında, sık yazılan mesaj ve arama kayıtları seyrek haber verir");
    }

    [Fact]
    public void Pencere_dolmadan_hicbir_sey_cikmaz()
    {
        var windows = new AdminChangeWindows();
        windows.Add(AdminListKinds.Comment, 0, 1, 0, T0);

        windows.TakeDue(T0.AddMilliseconds(999)).Should().BeEmpty();
        windows.NextDueAt.Should().Be(T0.AddSeconds(1));
    }

    [Fact]
    public void Ayni_pencerede_tur_ve_kimlik_bazinda_toplanir()
    {
        var windows = new AdminChangeWindows();
        windows.Add(AdminListKinds.Comment, 0, 1, 0, T0);
        windows.Add(AdminListKinds.Comment, 0, 2, 0, T0.AddMilliseconds(300));
        windows.Add(AdminListKinds.Comment, 7, 0, 1, T0.AddMilliseconds(600));

        windows.TakeDue(T0.AddSeconds(1)).Should().BeEquivalentTo(new[]
        {
            new AdminListChangeDto(AdminListKinds.Comment, 0, 3, 0),
            new AdminListChangeDto(AdminListKinds.Comment, 7, 0, 1)
        }, "kendi işlemini ayırt edebilmesi için istemci değişiklikleri kimliğe göre ayrık görmeli");
    }

    [Fact]
    public void Sabit_pencere_sonraki_degisikliklerle_uzamaz()
    {
        var windows = new AdminChangeWindows();
        windows.Add(AdminListKinds.Message, 0, 1, 0, T0);

        for (var i = 1; i <= 20; i++)
            windows.Add(AdminListKinds.Message, 0, 1, 0, T0.AddMilliseconds(i * 490));

        windows.NextDueAt.Should().Be(T0.AddSeconds(10), "sürekli akan mesaj trafiği gönderimi sonsuza dek erteleyememeli");
        windows.TakeDue(T0.AddSeconds(10)).Should().ContainSingle().Which.Added.Should().Be(21);
    }

    [Fact]
    public void Yalnizca_suresi_dolan_turler_cikar_ve_pencere_temizlenir()
    {
        var windows = new AdminChangeWindows();
        windows.Add(AdminListKinds.Comment, 0, 1, 0, T0);
        windows.Add(AdminListKinds.Message, 0, 1, 0, T0);

        windows.TakeDue(T0.AddSeconds(1)).Select(c => c.Kind).Should().Equal(AdminListKinds.Comment);
        windows.NextDueAt.Should().Be(T0.AddSeconds(10));

        windows.TakeDue(T0.AddSeconds(10)).Select(c => c.Kind).Should().Equal(AdminListKinds.Message);
        windows.NextDueAt.Should().BeNull();
        windows.TakeDue(T0.AddSeconds(20)).Should().BeEmpty("çıkan pencere ikinci kez gönderilmemeli");
    }

    [Fact]
    public void Gonderilen_pencereden_sonra_gelen_degisiklik_yeni_pencere_acar()
    {
        var windows = new AdminChangeWindows();
        windows.Add(AdminListKinds.Comment, 0, 1, 0, T0);
        windows.TakeDue(T0.AddSeconds(1));

        windows.Add(AdminListKinds.Comment, 0, 1, 0, T0.AddSeconds(5));

        windows.NextDueAt.Should().Be(T0.AddSeconds(6));
    }
}
