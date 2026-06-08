using InstituteService.Models;
using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/branches")]
public sealed class BranchesController(IInstituteAppService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<Branch>>.Ok(service.ListBranches(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost]
    public IActionResult Create(BranchRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<Branch>.Ok(service.CreateBranch(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
