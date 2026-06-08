using BillingService.Models;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing/refunds")]
public sealed class RefundsController(IBillingAppService service) : ControllerBase
{
    [HttpPost]
    public IActionResult Refund(RefundRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.Refund(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
