using InstituteService.Models;
using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/batches")]
public sealed class BatchesController(IInstituteAppService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<Batch>>.Ok(service.ListBatches(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost]
    public IActionResult Create(BatchRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<Batch>.Ok(service.CreateBatch(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
