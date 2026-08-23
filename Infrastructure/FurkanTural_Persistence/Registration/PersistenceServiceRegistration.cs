using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Interceptors;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FurkanTural_Persistence.Registration;

/// <summary>Veri katmanının bileşim kökü. Dışarıya yalnızca <see cref="IUnitOfWork"/> açılır; repo arayüzlerinin hiçbiri kapsayıcıya kaydedilmez, dolayısıyla IRepository&lt;T&gt; veya IBlogRepository doğrudan enjekte edilemez, hepsine UnitOfWork üzerinden gidilir.<para>Bağlantı dizesi kayıt anında bir kez okunup kapanışta tutulur, her istekte yeniden çözülmez. Buradan bir sıra bağımlılığı doğar: şifreli yapılandırmayı çözen adım bu çağrıdan önce koşmalıdır, yoksa kapanışa şifreli metin girer ve hata ancak ilk veri tabanı erişiminde ortaya çıkar.</para><para><see cref="AuditSaveChangesInterceptor"/> tekil, bağlam ise istek kapsamlıdır. Damgalayıcının ihtiyaç duyduğu saat kaynağı burada kaydedilmediği için bu metot tek başına çalışan bir kurulum vermez; iş katmanının kaydı da yapılmalıdır.</para></summary>
public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırılmamış.");

        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddDbContext<FurkanTuralDbContext>((sp, options) =>
            options.UseSqlServer(connectionString)
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
