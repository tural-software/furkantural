using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace FurkanTural_Business.Registration;

/// <summary>Yalnızca bu derlemedeki servisleri kaydeder. Bağımlılıklarının bir kısmı burada kurulmaz ve çağıranın sorumluluğunda kalır: <see cref="IUnitOfWork"/> (Persistence), <see cref="IPresenceTracker"/> ile <see cref="IChatNotifier"/> (SignalR gerektirdiği için API'de), <c>IHttpContextAccessor</c>, <c>IHttpClientFactory</c>, <c>IOptions&lt;AppTokenSettings&gt;</c> ve somut olarak çözülen <see cref="FurkanTural_Application.Settings.FileStorageSettings"/>. Eksikleri bu metot değil, ilk çözümleme anında DI fark ettirir.<para>Durum tutan sınıflar singleton'dır: hız sınırlayıcılar ve giriş kilidi sayaçlarını, saat ise saat dilimini bir kez çözüp süreç boyunca taşır. <see cref="IMessageProtector"/> de singleton'dır ve yapıcısı eksik anahtarda istisna fırlattığından, yapılandırma hatası uygulama açılışında değil o servisin ilk çözümlendiği istekte yüzeye çıkar.</para></summary>
public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ActivityLogger>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogImageService, BlogImageService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IEducationService, EducationService>();
        services.AddScoped<IExperienceService, ExperienceService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IMusicService, MusicService>();
        services.AddScoped<IMusicImageService, MusicImageService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectImageService, ProjectImageService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<ISubscriberService, SubscriberService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountActivationService, AccountActivationService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<ITurnstileVerifier, TurnstileVerifier>();
        services.AddScoped<IContactTemplateService, ContactTemplateService>();
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IUserFriendService, UserFriendService>();
        services.AddScoped<IChatMessageService, ChatMessageService>();
        services.AddScoped<ICallLogService, CallLogService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITurnCredentialProvider, TurnCredentialProvider>();
        services.AddScoped<ICallPolicyService, CallPolicyService>();
        services.AddSingleton<ICallRateLimiter, CallRateLimiter>();
        services.AddSingleton<IMessageRateLimiter, MessageRateLimiter>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ILoginThrottle, LoginThrottle>();
        services.AddSingleton<IMessageProtector, MessageProtector>();
        services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
        services.AddScoped<IPushSender, PushSender>();

        return services;
    }
}
