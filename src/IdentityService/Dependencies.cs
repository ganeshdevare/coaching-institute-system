using IdentityService.Repositories;
using IdentityService.Services;
using SharedKernel;
using SharedKernel.Configuration;

namespace IdentityService;

public static class Dependencies
{
    public static void ConfigureApp(this ConfigurationManager configuration)
    {
        configuration.SetBasePath(AppContext.BaseDirectory);
    }

    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfig? appConfig = configuration.GetSection(AppConfig.Name).Get<AppConfig>();
        if (appConfig is not null) services.AddSingleton(appConfig);

        RabbitMqConfig? rabbitMqConfig = configuration.GetSection(RabbitMqConfig.Name).Get<RabbitMqConfig>();
        if (rabbitMqConfig is not null) services.AddSingleton(rabbitMqConfig);

        // Services
        services.AddScoped<IIdentityAppService, global::IdentityService.Services.IdentityAppService>();

        // Repositories
        services.AddScoped<IInstituteRepository, InstituteRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
