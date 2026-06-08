using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/tenant")]
public sealed class TenantController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTenant()
        => Ok(ApiResponse<TenantContext>.Ok((TenantContext)HttpContext.Items["Tenant"]!));
}
