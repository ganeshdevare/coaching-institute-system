using System.Text;
using BillingService.Models;
using BillingService.Repositories;
using SharedKernel;

namespace BillingService.Services;

public sealed class BillingAppService : BaseService, IBillingAppService
{
    private readonly IBillingRepository billingRepository;
    private readonly IEventPublisher events;
    private readonly IClock clock;

    public BillingAppService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        billingRepository = GetService<IBillingRepository>();
        events = GetService<IEventPublisher>();
        clock = GetService<IClock>();
    }

    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public IEnumerable<FeePlan> ListFeePlans(TenantContext tenant)
        => billingRepository.ListFeePlans(tenant.InstituteId!.Value);

    public FeePlan CreateFeePlan(TenantContext tenant, FeePlanRequest request)
    {
        var plan = new FeePlan(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.Name, request.Amount, request.Installments, true);
        billingRepository.AddFeePlan(plan);
        return plan;
    }

    public IEnumerable<Payment> ListPayments(TenantContext tenant, Guid? studentId)
        => billingRepository.ListPayments(tenant.InstituteId!.Value, studentId);

    public object RecordPayment(TenantContext tenant, PaymentRequest request)
    {
        if (billingRepository.GetPaymentByIdempotencyKey(request.IdempotencyKey) is { } existing)
        {
            return new { duplicate = true, payment = existing };
        }

        var payment = new Payment(Guid.NewGuid(), tenant.InstituteId!.Value, request.StudentId, request.Amount, 0, request.Mode, "Completed", request.IdempotencyKey, clock.UtcNow);
        billingRepository.AddPayment(payment);
        events.Publish(tenant.InstituteId, EventNames.PaymentReceived, new { payment.Id, payment.StudentId, payment.Amount });
        return new { duplicate = false, payment };
    }

    public object HandleWebhook(TenantContext tenant, MockPaymentWebhook request)
        => RecordPayment(tenant, new PaymentRequest(request.StudentId, request.Amount, "MockProvider", request.IdempotencyKey));

    public object Refund(TenantContext tenant, RefundRequest request)
    {
        var payment = billingRepository.GetPayment(request.PaymentId);
        if (payment is null || payment.InstituteId != tenant.InstituteId)
        {
            throw new InvalidOperationException("Payment not found.");
        }

        if (request.Amount <= 0 || request.Amount > payment.Amount - payment.RefundedAmount)
        {
            throw new InvalidOperationException("Invalid refund amount.");
        }

        var updated = payment with
        {
            RefundedAmount = payment.RefundedAmount + request.Amount,
            Status = payment.RefundedAmount + request.Amount == payment.Amount ? "Refunded" : "PartiallyRefunded"
        };

        billingRepository.UpdatePayment(updated);
        events.Publish(tenant.InstituteId, EventNames.RefundProcessed, new { payment.Id, request.Amount, request.Reason });
        return updated;
    }

    public object BillingHistory(TenantContext tenant, Guid studentId)
        => billingRepository.BillingHistory(tenant.InstituteId!.Value, studentId);

    public byte[] ReceiptPdf(TenantContext tenant, Guid paymentId)
    {
        var payment = billingRepository.GetPayment(paymentId);
        if (payment is null || payment.InstituteId != tenant.InstituteId)
        {
            throw new InvalidOperationException("Payment not found.");
        }

        var body = $"Receipt\nPayment: {payment.Id}\nStudent: {payment.StudentId}\nAmount: {payment.Amount}\nDate: {payment.CreatedUtc:O}";
        return Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 0>>endobj\n% " + body + "\n%%EOF");
    }

    public ReportJob RequestReport(TenantContext tenant, ReportRequest request)
    {
        var report = new ReportJob(Guid.NewGuid(), tenant.InstituteId!.Value, request.ReportType, "Queued", null, clock.UtcNow);
        billingRepository.AddReport(report);
        events.Publish(tenant.InstituteId, EventNames.ReportRequested, new { report.Id, report.ReportType });
        return report;
    }

    public IEnumerable<ReportJob> ListReports(TenantContext tenant)
        => billingRepository.ListReports(tenant.InstituteId!.Value);

    public ReportJob? GetReport(Guid reportId) => billingRepository.GetReport(reportId);

    public IEnumerable<NotificationRecord> ListNotifications(TenantContext tenant)
        => billingRepository.ListNotifications(tenant.InstituteId!.Value);

    public object RequestReminder(TenantContext tenant, FeeReminderRequest request)
    {
        events.Publish(tenant.InstituteId, EventNames.FeeReminderRequested, new { request.StudentId });
        return new { queued = true, request.StudentId };
    }
}
