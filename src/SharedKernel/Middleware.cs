using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SharedKernel;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next(context);
    }
}

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, AppDataStore store)
    {
        var host = context.Request.Host.Host;
        var subdomain = host.EndsWith(".coachapp.local", StringComparison.OrdinalIgnoreCase)
            ? host[..^".coachapp.local".Length]
            : context.Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();

        var institute = store.Institutes.Values.FirstOrDefault(x => string.Equals(x.Subdomain, subdomain, StringComparison.OrdinalIgnoreCase));
        context.Items["Tenant"] = institute is null
            ? new TenantContext(null, subdomain, false)
            : new TenantContext(institute.Id, institute.Subdomain, true);

        await next(context);
    }
}

public sealed class JwtAuthenticationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, IJwtTokenService tokenService, IOptions<JwtOptions> options, IClock clock)
    {
        var header = context.Request.Headers.Authorization.FirstOrDefault();
        if (header?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var principal = tokenService.Validate(header["Bearer ".Length..].Trim(), options.Value, clock);
            if (principal is not null) context.User = principal;
        }

        await next(context);
    }
}

public sealed class ApiAuditMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, AppDataStore store, IClock clock)
    {
        await next(context);
        var tenant = context.Items["Tenant"] as TenantContext;
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");
        var audit = new AuditLog(Guid.NewGuid(), correlationId, tenant?.InstituteId, context.Request.Method, context.Request.Path, context.Response.StatusCode, context.User.Email(), clock.UtcNow);
        store.AuditLogs[audit.Id] = audit;
    }
}

public static class SharedApplicationBuilderExtensions
{
    public static IServiceCollection AddCoachSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddCors(options =>
        {
            options.AddPolicy("CoachCors", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                var configuredOrigins = origins
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("__", StringComparison.Ordinal))
                    .ToArray();

                if (configuredOrigins.Length > 0)
                {
                    policy.WithOrigins(configuredOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
                else
                {
                    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
                }
            });
        });
        services.AddSingleton<AppDataStore>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        return services;
    }

    public static IApplicationBuilder UseCoachPlatform(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCors("CoachCors");
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.UseMiddleware<ApiAuditMiddleware>();
        return app;
    }
}
