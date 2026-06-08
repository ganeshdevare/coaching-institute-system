using BillingService.Models;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing/fee-plans")]
public sealed class FeePlansController : ControllerBase
{
    private readonly IBillingAppService service;

    public FeePlansController(IBillingAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<FeePlan>>.Ok(service.ListFeePlans(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost]
    public IActionResult Create(FeePlanRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<FeePlan>.Ok(service.CreateFeePlan(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
