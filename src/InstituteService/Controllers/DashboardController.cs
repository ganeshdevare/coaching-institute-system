using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/dashboard")]
public sealed class DashboardController(IInstituteAppService service) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.Dashboard(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
