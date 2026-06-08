using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IBillingAppService service;

    public NotificationsController(IBillingAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public IActionResult List()
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<NotificationRecord>>.Ok(service.ListNotifications(tenant)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
