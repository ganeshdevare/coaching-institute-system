using BillingService.Models;
using SharedKernel;

namespace BillingService.Services;

public interface IBillingAppService
{
    bool RequireTenant(HttpContext http, out TenantContext tenant);
    IEnumerable<FeePlan> ListFeePlans(TenantContext tenant);
    FeePlan CreateFeePlan(TenantContext tenant, FeePlanRequest request);
    IEnumerable<Payment> ListPayments(TenantContext tenant, Guid? studentId);
    object RecordPayment(TenantContext tenant, PaymentRequest request);
    object HandleWebhook(TenantContext tenant, MockPaymentWebhook request);
    object Refund(TenantContext tenant, RefundRequest request);
    object BillingHistory(TenantContext tenant, Guid studentId);
    byte[] ReceiptPdf(TenantContext tenant, Guid paymentId);
    IEnumerable<ReportJob> ListReports(TenantContext tenant);
    ReportJob RequestReport(TenantContext tenant, ReportRequest request);
    ReportJob? GetReport(Guid reportId);
    IEnumerable<NotificationRecord> ListNotifications(TenantContext tenant);
    object RequestReminder(TenantContext tenant, FeeReminderRequest request);
}
