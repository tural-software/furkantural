using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Interceptors;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FurkanTural_Persistence.Registration;

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