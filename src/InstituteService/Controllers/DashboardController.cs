using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IInstituteAppService service;

    public DashboardController(IInstituteAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public IActionResult Get()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.Dashboard(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
