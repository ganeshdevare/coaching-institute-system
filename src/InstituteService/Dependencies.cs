using InstituteService.Repositories;
using InstituteService.Services;
using SharedKernel.Configuration;

namespace InstituteService;

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
        services.AddScoped<IInstituteAppService, global::InstituteService.Services.InstituteAppService>();

        // Repositories
        services.AddScoped<IInstituteRepository, InstituteRepository>();

        return services;
    }
}
