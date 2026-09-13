using System.Reflection;
using FluentAssertions;
using FurkanTural_API.Controllers.Base;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Tests;

/// <summary>Hub'lar controller değildir ve rota taramasına girmezler; yetkilendirmeleri unutulursa hiçbir mevcut test bunu yakalamaz. Oysa bağlantı bir kez kurulduğunda sunucu o istemciye yayın yapmaya başlar — yönetici kuyruğunun sayıları, yetkisiz bir dinleyiciye sızacak yer burasıdır.<para>Yönetici hub'ı ayrıca hedeflemesini gruba değil <c>Clients.All</c>'a yapar: o hub'a yalnızca yönetici bağlanabildiği için "hepsi" zaten "tüm yöneticiler" demektir. Bu kısayolun doğruluğu tamamen aşağıdaki politikaya bağlıdır.</para></summary>
public class HubPolicyTests
{
    private static readonly Dictionary<string, string[]> ClientMethods = new()
    {
        ["AdminHub"] = ["RefreshPendingWork"],
        ["ChatHub"] = ["SendMessage", "Typing", "CallUser", "AnswerCall", "SendIceCandidate", "NotifyMediaState", "RejectCall", "CancelCall", "HangUp"]
    };

    private static readonly Type[] AdminOwnedServices =
    [
        typeof(IBlogService),
        typeof(IBlogImageService),
        typeof(ICategoryService),
        typeof(ITagService),
        typeof(IProjectService),
        typeof(IProjectImageService),
        typeof(IMusicService),
        typeof(IMusicImageService),
        typeof(ISkillService),
        typeof(IExperienceService),
        typeof(IEducationService),
        typeof(IMailTemplateService),
        typeof(IMailTemplateTypeService),
        typeof(IRoleService),
        typeof(IStatusService),
        typeof(INewsletterIssueService),
        typeof(ICallPolicyService)
    ];

    private static IEnumerable<Type> Hubs() =>
        typeof(BaseApiController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t));

    private static string? PolicyOf(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).FirstOrDefault();

    private static IEnumerable<string> CallableMethods(Type hub) =>
        hub.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Where(m => m.GetBaseDefinition().DeclaringType == hub)
            .Select(m => m.Name)
            .Distinct();

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

    [Fact]
    public void Istemcinin_cagirabildigi_hub_metotlari_bilinen_listeyle_ortusur()
    {
        var hubs = Hubs().ToList();

        var actual = hubs
            .SelectMany(h => CallableMethods(h).Select(m => $"{h.Name}.{m}"))
            .ToHashSet();

        var expected = ClientMethods
            .SelectMany(x => x.Value.Select(m => $"{x.Key}.{m}"))
            .ToHashSet();

        actual.Should().NotBeEmpty("çağrılabilir metot bulunamıyorsa bu test hiçbir şeyi doğrulamıyor demektir");

        var unexpected = actual.Except(expected).Order().ToList();
        var stale = expected.Except(actual).Order().ToList();

        unexpected.Should().BeEmpty(
            "hub'daki her public metot bağlı her istemcinin çağırabileceği bir uçtur; yeni metot bilinçli bir karardır ve listeye adıyla girer:" +
            Environment.NewLine + string.Join(Environment.NewLine, unexpected));

        stale.Should().BeEmpty(
            "listede duran ama hub'da artık olmayan metot listeyi yanıltıcı yapar; kaldırılan metot listeden de çıkarılır:" +
            Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void Hublar_yonetici_kayitlarinin_servislerine_baglanmaz()
    {
        var sapan = Hubs()
            .SelectMany(h => h.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Where(p => AdminOwnedServices.Contains(p.ParameterType))
                .Select(p => $"{h.Name} → {p.ParameterType.Name}"))
            .ToList();

        sapan.Should().BeEmpty(
            "hub'a bağlanan istemci yönetici olmayabilir; yöneticiye ait bir servis hub'a enjekte edilirse o kayıtları değiştirecek yol HTTP yetki korumasını atlar:" +
            Environment.NewLine + string.Join(Environment.NewLine, sapan));
    }
}
