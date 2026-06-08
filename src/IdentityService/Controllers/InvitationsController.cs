using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IIdentityAppService service;

    public InvitationsController(IIdentityAppService service)
    {
        this.service = service;
    }

    [HttpPost]
    public IActionResult Invite(InviteUserRequest request)
        => IsAuthorized(Roles.InstituteOwner, Roles.InstituteAdmin, Roles.SuperAdmin)
            ? Ok(ApiResponse<object>.Ok(service.Invite(request, User), "Invitation created"))
            : Forbid();

    private bool IsAuthorized(params string[] roles)
        => User.Identity?.IsAuthenticated == true && roles.Contains(User.Role());
}
