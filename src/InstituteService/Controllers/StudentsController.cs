using InstituteService.Models;
using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute")]
public sealed class StudentsController(IInstituteAppService service) : ControllerBase
{
    [HttpGet("students/enrollments")]
    public IActionResult Enrollments([FromQuery] Guid? batchId, [FromQuery] Guid? studentId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<Enrollment>>.Ok(service.ListEnrollments(tenant, batchId, studentId)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost("students/enroll")]
    public IActionResult Enroll(EnrollRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.Enroll(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpGet("guardians")]
    public IActionResult GuardianMaps([FromQuery] Guid? studentId, [FromQuery] Guid? parentId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<GuardianMap>>.Ok(service.ListGuardianMaps(tenant, studentId, parentId)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost("guardians")]
    public IActionResult MapGuardian(GuardianRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<GuardianMap>.Ok(service.MapGuardian(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
