using System.Reflection;
using FluentAssertions;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Tests;

/// <summary>Hub'lar controller değildir ve rota taramasına girmezler; yetkilendirmeleri unutulursa hiçbir mevcut test bunu yakalamaz. Oysa bağlantı bir kez kurulduğunda sunucu o istemciye yayın yapmaya başlar — yönetici kuyruğunun sayıları, yetkisiz bir dinleyiciye sızacak yer burasıdır.<para>Yönetici hub'ı ayrıca hedeflemesini gruba değil <c>Clients.All</c>'a yapar: o hub'a yalnızca yönetici bağlanabildiği için "hepsi" zaten "tüm yöneticiler" demektir. Bu kısayolun doğruluğu tamamen aşağıdaki politikaya bağlıdır.</para></summary>
public class HubPolicyTests
{
    private static IEnumerable<Type> Hubs() =>
        typeof(BaseApiController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t));

    private static string? PolicyOf(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).FirstOrDefault();

    [Fact]
    public void Taramanin_kendisi_bos_donmez()
    {
        Hubs().Should().NotBeEmpty("hub bulunamazsa aşağıdaki denetimler sessizce geçer");
    }

    [Fact]
    public void Her_hub_bir_yetki_politikasi_tasir()
    {
        var sapan = Hubs().Where(h => string.IsNullOrEmpty(PolicyOf(h))).Select(h => h.Name).ToList();

        sapan.Should().BeEmpty("politikasız bir hub'a herkes bağlanabilir ve yayınlanan her şeyi dinler");
    }

    [Fact]
    public void Yonetici_hubina_yalnizca_yonetici_baglanabilir()
    {
        var hub = Hubs().SingleOrDefault(h => h.Name == "AdminHub");

        hub.Should().NotBeNull("yönetici bildirimlerini taşıyan hub kaldırıldıysa bu testin de gözden geçirilmesi gerekir");
        PolicyOf(hub!).Should().Be("AdminOnly",
            "hub Clients.All ile yayın yapıyor; politika gevşerse bekleyen iş sayıları yönetici olmayan bir dinleyiciye gider");
    }
}
