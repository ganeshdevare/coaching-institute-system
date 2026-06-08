using System.Text;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddScoped<BillingAppService>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCoachPlatform();

var group = app.MapGroup("/api/v1/billing").WithTags("Billing");

group.MapPost("/fee-plans", (FeePlanRequest request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<FeePlan>.Ok(service.CreateFeePlan(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/payments", (PaymentRequest request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.RecordPayment(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/payments/webhook/mock", (MockPaymentWebhook request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.HandleWebhook(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/refunds", (RefundRequest request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.Refund(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapGet("/students/{studentId:guid}/history", (Guid studentId, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.BillingHistory(tenant, studentId)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapGet("/payments/{paymentId:guid}/receipt.pdf", (Guid paymentId, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.File(service.ReceiptPdf(tenant, paymentId), "application/pdf", $"receipt-{paymentId:N}.pdf")
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapPost("/reports", (ReportRequest request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<ReportJob>.Ok(service.RequestReport(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

group.MapGet("/reports/{reportId:guid}", (Guid reportId, BillingAppService service)
    => service.GetReport(reportId) is { } report
        ? Results.Ok(ApiResponse<ReportJob>.Ok(report))
        : Results.NotFound(ApiResponse<object>.Fail("Report not found")));

group.MapPost("/reminders", (FeeReminderRequest request, HttpContext http, BillingAppService service)
    => service.RequireTenant(http, out var tenant)
        ? Results.Ok(ApiResponse<object>.Ok(service.RequestReminder(tenant, request)))
        : Results.BadRequest(ApiResponse<object>.Fail("Tenant not resolved")));

app.MapHealthChecks("/health");
app.Run();

public sealed record FeePlanRequest(Guid CourseId, string Name, decimal Amount, int Installments);
public sealed record PaymentRequest(Guid StudentId, decimal Amount, string Mode, string IdempotencyKey);
public sealed record MockPaymentWebhook(Guid StudentId, decimal Amount, string ProviderReference, string IdempotencyKey);
public sealed record RefundRequest(Guid PaymentId, decimal Amount, string Reason);
public sealed record ReportRequest(string ReportType);
public sealed record FeeReminderRequest(Guid StudentId);

public sealed class BillingAppService(AppDataStore store, IEventPublisher events, IClock clock)
{
    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public FeePlan CreateFeePlan(TenantContext tenant, FeePlanRequest request)
    {
        var plan = new FeePlan(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.Name, request.Amount, request.Installments, true);
        store.FeePlans[plan.Id] = plan;
        return plan;
    }

    public object RecordPayment(TenantContext tenant, PaymentRequest request)
    {
        if (store.Payments.Values.Any(x => x.IdempotencyKey == request.IdempotencyKey))
        {
            return new { duplicate = true, payment = store.Payments.Values.First(x => x.IdempotencyKey == request.IdempotencyKey) };
        }

        var payment = new Payment(Guid.NewGuid(), tenant.InstituteId!.Value, request.StudentId, request.Amount, 0, request.Mode, "Completed", request.IdempotencyKey, clock.UtcNow);
        store.Payments[payment.Id] = payment;
        events.Publish(tenant.InstituteId, EventNames.PaymentReceived, new { payment.Id, payment.StudentId, payment.Amount });
        return new { duplicate = false, payment };
    }

    public object HandleWebhook(TenantContext tenant, MockPaymentWebhook request)
        => RecordPayment(tenant, new PaymentRequest(request.StudentId, request.Amount, "MockProvider", request.IdempotencyKey));

    public object Refund(TenantContext tenant, RefundRequest request)
    {
        if (!store.Payments.TryGetValue(request.PaymentId, out var payment) || payment.InstituteId != tenant.InstituteId) throw new InvalidOperationException("Payment not found.");
        if (request.Amount <= 0 || request.Amount > payment.Amount - payment.RefundedAmount) throw new InvalidOperationException("Invalid refund amount.");
        var updated = payment with { RefundedAmount = payment.RefundedAmount + request.Amount, Status = payment.RefundedAmount + request.Amount == payment.Amount ? "Refunded" : "PartiallyRefunded" };
        store.Payments[payment.Id] = updated;
        events.Publish(tenant.InstituteId, EventNames.RefundProcessed, new { payment.Id, request.Amount, request.Reason });
        return updated;
    }

    public object BillingHistory(TenantContext tenant, Guid studentId)
    {
        var paid = store.Payments.Values.Where(x => x.InstituteId == tenant.InstituteId && x.StudentId == studentId).ToList();
        var planned = store.FeePlans.Values.Where(x => x.InstituteId == tenant.InstituteId).Sum(x => x.Amount);
        var netPaid = paid.Sum(x => x.Amount - x.RefundedAmount);
        return new { studentId, planned, netPaid, pending = Math.Max(0, planned - netPaid), creditBalance = Math.Max(0, netPaid - planned), payments = paid };
    }

    public byte[] ReceiptPdf(TenantContext tenant, Guid paymentId)
    {
        var payment = store.Payments.Values.FirstOrDefault(x => x.Id == paymentId && x.InstituteId == tenant.InstituteId) ?? throw new InvalidOperationException("Payment not found.");
        var body = $"Receipt\nPayment: {payment.Id}\nStudent: {payment.StudentId}\nAmount: {payment.Amount}\nDate: {payment.CreatedUtc:O}";
        return Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 0>>endobj\n% " + body + "\n%%EOF");
    }

    public ReportJob RequestReport(TenantContext tenant, ReportRequest request)
    {
        var report = new ReportJob(Guid.NewGuid(), tenant.InstituteId!.Value, request.ReportType, "Queued", null, clock.UtcNow);
        store.ReportJobs[report.Id] = report;
        events.Publish(tenant.InstituteId, EventNames.ReportRequested, new { report.Id, report.ReportType });
        return report;
    }

    public ReportJob? GetReport(Guid reportId) => store.ReportJobs.GetValueOrDefault(reportId);

    public object RequestReminder(TenantContext tenant, FeeReminderRequest request)
    {
        events.Publish(tenant.InstituteId, EventNames.FeeReminderRequested, new { request.StudentId });
        return new { queued = true, request.StudentId };
    }
}
