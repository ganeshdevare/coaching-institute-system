using System.Security.Claims;
using IdentityService.Models;
using SharedKernel;

namespace IdentityService.Services;

public interface IIdentityAppService
{
    IEnumerable<Institute> ListInstitutes();
    IEnumerable<UserSummaryResponse> ListUsers(Guid? instituteId);
    object RegisterInstitute(RegisterInstituteRequest request);
    object? Login(LoginRequest request);
    object? Refresh(RefreshRequest request);
    object Invite(InviteUserRequest request, ClaimsPrincipal principal);
    bool ChangePassword(ChangePasswordRequest request, string email);
    object ResetPassword(ResetPasswordRequest request);
}
