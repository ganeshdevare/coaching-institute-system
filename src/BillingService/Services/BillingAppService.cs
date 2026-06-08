using System.Text;
using BillingService.Models;
using SharedKernel;

namespace BillingService.Services;

public sealed class BillingAppService(AppDataStore store, IEventPublisher events, IClock clock) : IBillingAppService
{
    public bool RequireTenant(HttpContext http, out TenantContext tenant)
    {
        tenant = (TenantContext?)http.Items["Tenant"] ?? new TenantContext(null, null, false);
        return tenant.IsResolved && tenant.InstituteId.HasValue;
    }

    public IEnumerable<FeePlan> ListFeePlans(TenantContext tenant)
        => store.FeePlans.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderBy(x => x.Name);

    public FeePlan CreateFeePlan(TenantContext tenant, FeePlanRequest request)
    {
        var plan = new FeePlan(Guid.NewGuid(), tenant.InstituteId!.Value, request.CourseId, request.Name, request.Amount, request.Installments, true);
        store.FeePlans[plan.Id] = plan;
        return plan;
    }

    public IEnumerable<Payment> ListPayments(TenantContext tenant, Guid? studentId)
        => store.Payments.Values
            .Where(x => x.InstituteId == tenant.InstituteId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .OrderByDescending(x => x.CreatedUtc);

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
        if (!store.Payments.TryGetValue(request.PaymentId, out var payment) || payment.InstituteId != tenant.InstituteId)
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

    public IEnumerable<ReportJob> ListReports(TenantContext tenant)
        => store.ReportJobs.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderByDescending(x => x.CreatedUtc);

    public ReportJob? GetReport(Guid reportId) => store.ReportJobs.GetValueOrDefault(reportId);

    public IEnumerable<NotificationRecord> ListNotifications(TenantContext tenant)
        => store.Notifications.Values.Where(x => x.InstituteId == tenant.InstituteId).OrderByDescending(x => x.CreatedUtc);

    public object RequestReminder(TenantContext tenant, FeeReminderRequest request)
    {
        events.Publish(tenant.InstituteId, EventNames.FeeReminderRequested, new { request.StudentId });
        return new { queued = true, request.StudentId };
    }
}
