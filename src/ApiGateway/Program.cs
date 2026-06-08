using Microsoft.AspNetCore.Server.Kestrel.Core;
using SharedKernel;
using System.Text.Json.Serialization;

namespace ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Configuration.ConfigureApp();

        ConfigureServices(builder);
        ConfigureApp(builder);
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddCoachSharedKernel(builder.Configuration);
        builder.Services.AddHttpClient();
        builder.Services.AddDependencies(builder.Configuration);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddHealthChecks();
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = null;
        });
    }

    private static void ConfigureApp(WebApplicationBuilder builder)
    {
        WebApplication app = builder.Build();

        app.UseCoachPlatform();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }
}
