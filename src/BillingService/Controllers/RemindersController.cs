using BillingService.Models;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing/reminders")]
public sealed class RemindersController(IBillingAppService service) : ControllerBase
{
    [HttpPost]
    public IActionResult RequestReminder(FeeReminderRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.RequestReminder(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
