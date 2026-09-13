using System.Reflection;
using FluentAssertions;
using FurkanTural_API.Controllers;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FurkanTural_API.Tests;

public class WriteEndpointPolicyTests
{
    private const string AdminOnly = "AdminOnly";
    private const string Anonymous = "Anonim";

    private static readonly Dictionary<string, string> OpenWrites = new()
    {
        ["AuthController.Login"] = Anonymous,
        ["AuthController.AppToken"] = Anonymous,
        ["AuthController.Register"] = Anonymous,
        ["AuthController.Activate"] = Anonymous,
        ["AuthController.Refresh"] = "UserOrAdmin",
        ["BlogController.RegisterView"] = "VisitorOrAbove",
        ["CommentController.Submit"] = "VisitorOrAbove",
        ["CommentController.DisableNotifications"] = "VisitorOrAbove",
        ["ContactController.Submit"] = "VisitorOrAbove",
        ["SubscriberController.Subscribe"] = "VisitorOrAbove",
        ["SubscriberController.Confirm"] = "VisitorOrAbove",
        ["SubscriberController.RequestUnsubscribe"] = "VisitorOrAbove",
        ["SubscriberController.Unsubscribe"] = "VisitorOrAbove",
        ["ClientLogController.Create"] = "AppClient",
        ["UserController.SeedAdmin"] = Anonymous,
        ["UserController.UploadMyAvatar"] = "UserOrAdmin",
        ["UserController.AcceptAgreement"] = "UserOrAdmin",
        ["UserController.DeactivateMyAccount"] = "UserOrAdmin",
        ["FriendController.SendRequest"] = "UserOrAdmin",
        ["FriendController.Accept"] = "UserOrAdmin",
        ["FriendController.Reject"] = "UserOrAdmin",
        ["FriendController.Remove"] = "UserOrAdmin",
        ["FriendController.Block"] = "UserOrAdmin",
        ["FriendController.Unblock"] = "UserOrAdmin",
        ["MessageController.Send"] = "UserOrAdmin",
        ["MessageController.SendAudio"] = "UserOrAdmin",
        ["MessageController.SendMedia"] = "UserOrAdmin",
        ["MessageController.DeleteOwn"] = "UserOrAdmin",
        ["MessageController.EditOwn"] = "UserOrAdmin",
        ["MessageController.MarkRead"] = "UserOrAdmin",
        ["ReportController.Create"] = "UserOrAdmin",
        ["PushController.Subscribe"] = "UserOrAdmin",
        ["PushController.Unsubscribe"] = "UserOrAdmin"
    };

    private static string Access(Type controller, MethodInfo action)
    {
        if (action.GetCustomAttributes<AllowAnonymousAttribute>().Any()
            || controller.GetCustomAttributes<AllowAnonymousAttribute>().Any())
            return Anonymous;

        var rules = controller.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(action.GetCustomAttributes<AuthorizeAttribute>())
            .Select(a => a.Policy ?? (a.Roles is null ? "Kimlikli" : "Roller:" + a.Roles))
            .Distinct()
            .Order()
            .ToList();

        if (rules.Count == 0) return Anonymous;
        return rules.Contains(AdminOnly) ? AdminOnly : string.Join(" + ", rules);
    }

    private static List<(string Key, string Verbs, string Access)> Writes()
    {
        var controllers = typeof(BaseApiController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        var writes = new List<(string, string, string)>();

        foreach (var controller in controllers)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var verbs = action.GetCustomAttributes<HttpMethodAttribute>()
                    .SelectMany(h => h.HttpMethods)
                    .Where(m => m is not "GET" and not "HEAD")
                    .Distinct()
                    .ToList();

                if (verbs.Count > 0)
                    writes.Add(($"{controller.Name}.{action.Name}", string.Join(",", verbs), Access(controller, action)));
            }
        }

        return writes;
    }

    [Fact]
    public void Taramanin_kendisi_bos_donmez()
    {
        Writes().Count.Should().BeGreaterThan(OpenWrites.Count,
            "yazma ucu bulunamıyorsa bu test hiçbir şeyi doğrulamıyor demektir");
    }

    private static readonly HashSet<string> CounterWrites = ["BlogController.RegisterView"];

    [Theory]
    [InlineData(nameof(BlogController))]
    [InlineData(nameof(BlogImageController))]
    [InlineData(nameof(CategoryController))]
    [InlineData(nameof(TagController))]
    [InlineData(nameof(ProjectController))]
    [InlineData(nameof(ProjectImageController))]
    [InlineData(nameof(MusicController))]
    [InlineData(nameof(MusicImageController))]
    [InlineData(nameof(SkillController))]
    [InlineData(nameof(ExperienceController))]
    [InlineData(nameof(EducationController))]
    [InlineData(nameof(MailTemplateController))]
    [InlineData(nameof(MailTemplateTypeController))]
    [InlineData(nameof(RoleController))]
    [InlineData(nameof(StatusController))]
    [InlineData(nameof(NewsletterIssueController))]
    [InlineData(nameof(CallController) + ".UpdatePolicy")]
    public void Yonetici_kayitlarini_yalnizca_yonetici_degistirir(string scope)
    {
        var writes = Writes()
            .Where(w => w.Key == scope || w.Key.StartsWith(scope + ".", StringComparison.Ordinal))
            .Where(w => !CounterWrites.Contains(w.Key))
            .ToList();

        writes.Should().NotBeEmpty($"'{scope}' için taranacak yazma ucu yoksa bu test hiçbir şeyi doğrulamıyor demektir");

        var acik = writes
            .Where(w => w.Access != AdminOnly)
            .Select(w => $"{w.Key} [{w.Verbs}] → {w.Access}")
            .ToList();

        acik.Should().BeEmpty(
            "bu kayıtları yalnızca yönetici ekler, günceller, siler, pasife alır ve geri yükler; kayıt olan her Chatural hesabı User rolü alır, " +
            "bu uçlardan biri o role açık kalırsa üye yönetici içeriğini değiştirebilir:" + Environment.NewLine + string.Join(Environment.NewLine, acik));
    }

    [Fact]
    public void Her_aksiyon_http_metodunu_acikca_bildirir()
    {
        var controllers = typeof(BaseApiController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        var belirsiz = controllers
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => !m.GetCustomAttributes<NonActionAttribute>().Any())
                .Where(m => !m.GetCustomAttributes().OfType<IActionHttpMethodProvider>().Any())
                .Select(m => $"{c.Name}.{m.Name}"))
            .ToList();

        belirsiz.Should().BeEmpty(
            "metot bildirmeyen aksiyon her HTTP metodunu kabul eder; yazma uçlarını tarayan korumalar onu göremez ve POST ile çağrılan bir uç gözden kaçar:" +
            Environment.NewLine + string.Join(Environment.NewLine, belirsiz));
    }

    [Fact]
    public void Yonetici_disina_acik_yazma_uclari_bilinen_listeyle_ortusur()
    {
        var actual = Writes()
            .Where(w => w.Access != AdminOnly)
            .GroupBy(w => w.Key)
            .ToDictionary(g => g.Key, g => string.Join(" | ", g.Select(w => w.Access).Distinct()));

        var unexpected = actual
            .Where(x => !OpenWrites.TryGetValue(x.Key, out var expected) || expected != x.Value)
            .Select(x => $"{x.Key} → {x.Value}")
            .ToList();

        var stale = OpenWrites
            .Where(x => !actual.ContainsKey(x.Key))
            .Select(x => x.Key)
            .ToList();

        unexpected.Should().BeEmpty(
            "yönetici olmayana açılan her yazma ucu bilinçli bir karardır ve listeye adıyla girer; listede olmayan açık bir uç unutulmuş bir özniteliktir:" +
            Environment.NewLine + string.Join(Environment.NewLine, unexpected));

        stale.Should().BeEmpty(
            "listede duran ama artık kapalı ya da kaldırılmış bir uç listeyi yanıltıcı yapar; kapatılan uç listeden de çıkarılır:" +
            Environment.NewLine + string.Join(Environment.NewLine, stale));
    }
}
