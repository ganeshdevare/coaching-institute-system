using InstituteService.Models;
using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/attendance")]
public sealed class AttendanceController(IInstituteAppService service) : ControllerBase
{
    [HttpPost]
    public IActionResult Mark(AttendanceRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.MarkAttendance(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpGet("summary")]
    public IActionResult Summary([FromQuery] Guid batchId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.AttendanceSummary(tenant, batchId)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
