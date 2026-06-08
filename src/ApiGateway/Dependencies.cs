using ApiGateway.Configuration;
using ApiGateway.Services;
using SharedKernel.Configuration;

namespace ApiGateway;

public static class Dependencies
{
    public static void ConfigureApp(this ConfigurationManager configuration)
    {
        configuration.SetBasePath(AppContext.BaseDirectory);
        configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);
    }

    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfig? appConfig = configuration.GetSection(AppConfig.Name).Get<AppConfig>();
        if (appConfig is not null) services.AddSingleton(appConfig);

        GatewayServiceConfig? serviceConfig = configuration.GetSection(GatewayServiceConfig.Name).Get<GatewayServiceConfig>();
        if (serviceConfig is not null) services.AddSingleton(serviceConfig);

        services.AddScoped<IGatewayProxyService, GatewayProxyService>();
        services.AddScoped<IOpenApiDocumentService, OpenApiDocumentService>();

        return services;
    }
}
