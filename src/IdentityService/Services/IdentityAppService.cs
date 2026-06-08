using System.Security.Claims;
using IdentityService.Models;
using IdentityService.Repositories;
using SharedKernel;

namespace IdentityService.Services;

public sealed class IdentityAppService : BaseService, IIdentityAppService
{
    private readonly IInstituteRepository instituteRepository;
    private readonly IUserRepository userRepository;
    private readonly ITokenRepository tokenRepository;
    private readonly IPasswordHasher hasher;
    private readonly IJwtTokenService jwt;
    private readonly IConfiguration config;
    private readonly IClock clock;

    public IdentityAppService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        instituteRepository = GetService<IInstituteRepository>();
        userRepository = GetService<IUserRepository>();
        tokenRepository = GetService<ITokenRepository>();
        hasher = GetService<IPasswordHasher>();
        jwt = GetService<IJwtTokenService>();
        config = GetService<IConfiguration>();
        clock = GetService<IClock>();
    }

    private JwtOptions JwtOptions => config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

    public IEnumerable<Institute> ListInstitutes()
        => instituteRepository.List();

    public IEnumerable<UserSummaryResponse> ListUsers(Guid? instituteId)
        => userRepository.List(instituteId);

    public object RegisterInstitute(RegisterInstituteRequest request)
    {
        var subdomain = NormalizeSubdomain(request.Subdomain);
        if (instituteRepository.GetBySubdomain(subdomain) is not null)
        {
            throw new InvalidOperationException("Subdomain already exists.");
        }

        var institute = new Institute(Guid.NewGuid(), request.Name, subdomain, request.OwnerEmail, "Active", clock.UtcNow);
        var owner = new AppUser(Guid.NewGuid(), institute.Id, request.OwnerEmail.ToLowerInvariant(), hasher.Hash(request.Password), request.OwnerName, Roles.InstituteOwner, "Active");
        var branch = new Branch(Guid.NewGuid(), institute.Id, "Main Branch", "Set address", true);

        instituteRepository.Add(institute);
        userRepository.Add(owner);
        instituteRepository.AddBranch(branch);

        return new { institute.Id, institute.Name, institute.Subdomain, ownerEmail = owner.Email, host = $"{institute.Subdomain}.coachapp.local" };
    }

    public object? Login(LoginRequest request)
    {
        var user = userRepository.GetByEmail(request.Email);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var refresh = new RefreshToken(Guid.NewGuid(), user.Id, Guid.NewGuid().ToString("N"), clock.UtcNow.AddDays(JwtOptions.RefreshTokenDays), false);
        tokenRepository.Add(refresh);
        return new { accessToken = jwt.CreateToken(user, JwtOptions, clock), refreshToken = refresh.Token, user.Email, user.Role, user.InstituteId };
    }

    public object? Refresh(RefreshRequest request)
    {
        var token = tokenRepository.GetValidRefreshToken(request.RefreshToken, clock.UtcNow);
        if (token is null || userRepository.GetById(token.UserId) is not { } user)
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
        userRepository.Add(user);
        return new { user.Id, user.Email, user.Role, temporaryPassword = password, status = user.Status };
    }

    public bool ChangePassword(ChangePasswordRequest request, string email)
    {
        var user = userRepository.GetByEmail(email);
        if (user is null || !hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        userRepository.Update(user with { PasswordHash = hasher.Hash(request.NewPassword), Status = "Active" });
        return true;
    }

    public object ResetPassword(ResetPasswordRequest request)
        => new { request.Email, resetToken = Guid.NewGuid().ToString("N"), expiresMinutes = 30 };

    private static string NormalizeSubdomain(string value)
        => value.Trim().ToLowerInvariant().Replace(" ", "-");
}
