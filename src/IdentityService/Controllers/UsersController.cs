using IdentityService.Services;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity/users")]
public sealed class UsersController(IIdentityAppService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] Guid? instituteId)
        => Ok(ApiResponse<IEnumerable<UserSummaryResponse>>.Ok(service.ListUsers(instituteId)));
}
