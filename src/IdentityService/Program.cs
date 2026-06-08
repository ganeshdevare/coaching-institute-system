using SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddScoped<IdentityAppService>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCoachPlatform();

SeedData.Ensure(app.Services);

var group = app.MapGroup("/api/v1/identity").WithTags("Identity");

group.MapPost("/institutes/register", (RegisterInstituteRequest request, IdentityAppService service)
    => Results.Ok(ApiResponse<object>.Ok(service.RegisterInstitute(request), "Institute registered")));

group.MapPost("/login", (LoginRequest request, IdentityAppService service)
    => service.Login(request) is { } result
        ? Results.Ok(ApiResponse<object>.Ok(result, "Login successful"))
        : Results.BadRequest(ApiResponse<object>.Fail("Invalid credentials or inactive account")));

group.MapPost("/refresh", (RefreshRequest request, IdentityAppService service)
    => service.Refresh(request) is { } result
        ? Results.Ok(ApiResponse<object>.Ok(result, "Token refreshed"))
        : Results.BadRequest(ApiResponse<object>.Fail("Invalid refresh token")));

group.MapPost("/invitations", (InviteUserRequest request, HttpContext http, IdentityAppService service)
    => Authorize(http, Roles.InstituteOwner, Roles.InstituteAdmin, Roles.SuperAdmin)
        ? Results.Ok(ApiResponse<object>.Ok(service.Invite(request, http.User), "Invitation created"))
        : Results.Forbid());

group.MapPost("/password/change", (ChangePasswordRequest request, HttpContext http, IdentityAppService service)
    => http.User.Identity?.IsAuthenticated == true && service.ChangePassword(request, http.User.Email())
        ? Results.Ok(ApiResponse<object>.Ok(new { changed = true }))
        : Results.BadRequest(ApiResponse<object>.Fail("Password change failed")));

group.MapPost("/password/reset", (ResetPasswordRequest request, IdentityAppService service)
    => Results.Ok(ApiResponse<object>.Ok(service.ResetPassword(request), "Mock reset token generated")));

group.MapGet("/me", (HttpContext http) => http.User.Identity?.IsAuthenticated == true
    ? Results.Ok(ApiResponse<object>.Ok(new { email = http.User.Email(), role = http.User.Role(), instituteId = http.User.InstituteId() }))
    : Results.Unauthorized());

group.MapGet("/audit/search", (string? path, string? correlationId, AppDataStore store) =>
{
    var data = store.AuditLogs.Values
        .Where(x => string.IsNullOrWhiteSpace(path) || x.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
        .Where(x => string.IsNullOrWhiteSpace(correlationId) || x.CorrelationId == correlationId)
        .OrderByDescending(x => x.CreatedUtc)
        .Take(100);
    return Results.Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(data));
});

app.MapHealthChecks("/health");
app.Run();

static bool Authorize(HttpContext http, params string[] roles)
    => http.User.Identity?.IsAuthenticated == true && roles.Contains(http.User.Role());

public sealed record RegisterInstituteRequest(string Name, string Subdomain, string OwnerEmail, string OwnerName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record InviteUserRequest(string Email, string DisplayName, string Role);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ResetPasswordRequest(string Email);

public sealed class IdentityAppService(AppDataStore store, IPasswordHasher hasher, IJwtTokenService jwt, IConfiguration config, IClock clock)
{
    private JwtOptions JwtOptions => config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

    public object RegisterInstitute(RegisterInstituteRequest request)
    {
        var subdomain = NormalizeSubdomain(request.Subdomain);
        if (store.Institutes.Values.Any(x => x.Subdomain == subdomain)) throw new InvalidOperationException("Subdomain already exists.");

        var institute = new Institute(Guid.NewGuid(), request.Name, subdomain, request.OwnerEmail, "Active", clock.UtcNow);
        var owner = new AppUser(Guid.NewGuid(), institute.Id, request.OwnerEmail.ToLowerInvariant(), hasher.Hash(request.Password), request.OwnerName, Roles.InstituteOwner, "Active");
        var branch = new Branch(Guid.NewGuid(), institute.Id, "Main Branch", "Set address", true);
        store.Institutes[institute.Id] = institute;
        store.Users[owner.Id] = owner;
        store.Branches[branch.Id] = branch;
        return new { institute.Id, institute.Name, institute.Subdomain, ownerEmail = owner.Email, host = $"{institute.Subdomain}.coachapp.local" };
    }

    public object? Login(LoginRequest request)
    {
        var user = store.Users.Values.FirstOrDefault(x => x.Email == request.Email.ToLowerInvariant() && x.Status == "Active");
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash)) return null;

        var refresh = new RefreshToken(Guid.NewGuid(), user.Id, Guid.NewGuid().ToString("N"), clock.UtcNow.AddDays(JwtOptions.RefreshTokenDays), false);
        store.RefreshTokens[refresh.Id] = refresh;
        return new { accessToken = jwt.CreateToken(user, JwtOptions, clock), refreshToken = refresh.Token, user.Email, user.Role, user.InstituteId };
    }

    public object? Refresh(RefreshRequest request)
    {
        var token = store.RefreshTokens.Values.FirstOrDefault(x => x.Token == request.RefreshToken && !x.Revoked && x.ExpiresUtc > clock.UtcNow);
        if (token is null || !store.Users.TryGetValue(token.UserId, out var user)) return null;
        return new { accessToken = jwt.CreateToken(user, JwtOptions, clock), refreshToken = token.Token };
    }

    public object Invite(InviteUserRequest request, System.Security.Claims.ClaimsPrincipal principal)
    {
        var instituteId = principal.Role() == Roles.SuperAdmin ? null : principal.InstituteId();
        if (request.Role == Roles.SuperAdmin) throw new InvalidOperationException("SuperAdmin cannot be invited from tenant scope.");
        var password = "Welcome@123";
        var user = new AppUser(Guid.NewGuid(), instituteId, request.Email.ToLowerInvariant(), hasher.Hash(password), request.DisplayName, request.Role, "Invited");
        store.Users[user.Id] = user;
        return new { user.Id, user.Email, user.Role, temporaryPassword = password, status = user.Status };
    }

    public bool ChangePassword(ChangePasswordRequest request, string email)
    {
        var user = store.Users.Values.FirstOrDefault(x => x.Email == email);
        if (user is null || !hasher.Verify(request.CurrentPassword, user.PasswordHash)) return false;
        store.Users[user.Id] = user with { PasswordHash = hasher.Hash(request.NewPassword), Status = "Active" };
        return true;
    }

    public object ResetPassword(ResetPasswordRequest request) => new { request.Email, resetToken = Guid.NewGuid().ToString("N"), expiresMinutes = 30 };

    private static string NormalizeSubdomain(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", "-");
}

public static class SeedData
{
    public static void Ensure(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<AppDataStore>();
        if (store.Users.Values.Any(x => x.Role == Roles.SuperAdmin)) return;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var superAdmin = new AppUser(Guid.NewGuid(), null, "superadmin@coachapp.local", hasher.Hash("Admin@123"), "System Super Admin", Roles.SuperAdmin, "Active");
        store.Users[superAdmin.Id] = superAdmin;
    }
}
