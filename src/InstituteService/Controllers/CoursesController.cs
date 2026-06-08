using InstituteService.Models;
using InstituteService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace InstituteService.Controllers;

[ApiController]
[Route("api/v1/institute/courses")]
public sealed class CoursesController : ControllerBase
{
    private readonly IInstituteAppService service;

    public CoursesController(IInstituteAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<Course>>.Ok(service.ListCourses(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost]
    public IActionResult Create(CourseRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<Course>.Ok(service.CreateCourse(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
