using BillingService.Models;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace BillingService.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IBillingAppService service;

    public PaymentsController(IBillingAppService service)
    {
        this.service = service;
    }

    [HttpGet("payments")]
    public IActionResult List([FromQuery] Guid? studentId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<IEnumerable<Payment>>.Ok(service.ListPayments(tenant, studentId)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost("payments")]
    public IActionResult Record(PaymentRequest request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.RecordPayment(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpPost("payments/webhook/mock")]
    public IActionResult MockWebhook(MockPaymentWebhook request)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.HandleWebhook(tenant, request)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpGet("students/{studentId:guid}/history")]
    public IActionResult History(Guid studentId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? Ok(ApiResponse<object>.Ok(service.BillingHistory(tenant, studentId)))
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));

    [HttpGet("payments/{paymentId:guid}/receipt.pdf")]
    public IActionResult Receipt(Guid paymentId)
        => service.RequireTenant(HttpContext, out var tenant)
            ? File(service.ReceiptPdf(tenant, paymentId), "application/pdf", $"receipt-{paymentId:N}.pdf")
            : BadRequest(ApiResponse<object>.Fail("Tenant not resolved"));
}
