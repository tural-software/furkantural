using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace FurkanTural_Business.Registration;

public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ActivityLogger>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogImageService, BlogImageService>();
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
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}