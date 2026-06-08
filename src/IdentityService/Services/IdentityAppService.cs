using System.Security.Claims;
using IdentityService.Models;
using SharedKernel;

namespace IdentityService.Services;

public sealed class IdentityAppService(AppDataStore store, IPasswordHasher hasher, IJwtTokenService jwt, IConfiguration config, IClock clock) : IIdentityAppService
{
    private JwtOptions JwtOptions => config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

    public IEnumerable<Institute> ListInstitutes()
        => store.Institutes.Values.OrderBy(x => x.Name);

    public IEnumerable<UserSummaryResponse> ListUsers(Guid? instituteId)
        => store.Users.Values
            .Where(x => !instituteId.HasValue || x.InstituteId == instituteId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserSummaryResponse(x.Id, x.InstituteId, x.Email, x.DisplayName, x.Role, x.Status));

    public object RegisterInstitute(RegisterInstituteRequest request)
    {
        var subdomain = NormalizeSubdomain(request.Subdomain);
        if (store.Institutes.Values.Any(x => x.Subdomain == subdomain))
        {
            throw new InvalidOperationException("Subdomain already exists.");
        }

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
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var refresh = new RefreshToken(Guid.NewGuid(), user.Id, Guid.NewGuid().ToString("N"), clock.UtcNow.AddDays(JwtOptions.RefreshTokenDays), false);
        store.RefreshTokens[refresh.Id] = refresh;
        return new { accessToken = jwt.CreateToken(user, JwtOptions, clock), refreshToken = refresh.Token, user.Email, user.Role, user.InstituteId };
    }

    public object? Refresh(RefreshRequest request)
    {
        var token = store.RefreshTokens.Values.FirstOrDefault(x => x.Token == request.RefreshToken && !x.Revoked && x.ExpiresUtc > clock.UtcNow);
        if (token is null || !store.Users.TryGetValue(token.UserId, out var user))
        {
            return null;
        }

        return new { accessToken = jwt.CreateToken(user, JwtOptions, clock), refreshToken = token.Token };
    }

    public object Invite(InviteUserRequest request, ClaimsPrincipal principal)
    {
        var instituteId = principal.Role() == Roles.SuperAdmin ? null : principal.InstituteId();
        if (request.Role == Roles.SuperAdmin)
        {
            throw new InvalidOperationException("SuperAdmin cannot be invited from tenant scope.");
        }

        var password = "Welcome@123";
        var user = new AppUser(Guid.NewGuid(), instituteId, request.Email.ToLowerInvariant(), hasher.Hash(password), request.DisplayName, request.Role, "Invited");
        store.Users[user.Id] = user;
        return new { user.Id, user.Email, user.Role, temporaryPassword = password, status = user.Status };
    }

    public bool ChangePassword(ChangePasswordRequest request, string email)
    {
        var user = store.Users.Values.FirstOrDefault(x => x.Email == email);
        if (user is null || !hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        store.Users[user.Id] = user with { PasswordHash = hasher.Hash(request.NewPassword), Status = "Active" };
        return true;
    }

    public object ResetPassword(ResetPasswordRequest request)
        => new { request.Email, resetToken = Guid.NewGuid().ToString("N"), expiresMinutes = 30 };

    private static string NormalizeSubdomain(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", "-");
}
