using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity/institutes")]
public sealed class InstitutesController(IIdentityAppService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List()
        => Ok(ApiResponse<IEnumerable<Institute>>.Ok(service.ListInstitutes()));

    [HttpPost("register")]
    public IActionResult Register(RegisterInstituteRequest request)
        => Ok(ApiResponse<object>.Ok(service.RegisterInstitute(request), "Institute registered"));
}
