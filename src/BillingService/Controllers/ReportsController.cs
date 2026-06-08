using BillingService.Models;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IBillingAppService service;

    public ReportsController(IBillingAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<ReportJob>>.Ok(service.ListReports(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost]
    public IActionResult RequestReport(ReportRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<ReportJob>.Ok(service.RequestReport(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpGet("{reportId:guid}")]
    public IActionResult Get(Guid reportId)
        => service.GetReport(reportId) is { } report
            ? Ok(ApiResponse<ReportJob>.Ok(report))
            : NotFound(ApiResponse<object>.Fail("Report not found"));
}
