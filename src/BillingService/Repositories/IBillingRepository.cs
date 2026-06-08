using SharedKernel;

namespace BillingService.Repositories;

public interface IBillingRepository
{
    IEnumerable<FeePlan> ListFeePlans(Guid instituteId);
    void AddFeePlan(FeePlan feePlan);
    IEnumerable<Payment> ListPayments(Guid instituteId, Guid? studentId);
    Payment? GetPayment(Guid paymentId);
    Payment? GetPaymentByIdempotencyKey(string idempotencyKey);
    void AddPayment(Payment payment);
    void UpdatePayment(Payment payment);
    object BillingHistory(Guid instituteId, Guid studentId);
    IEnumerable<ReportJob> ListReports(Guid instituteId);
    void AddReport(ReportJob reportJob);
    ReportJob? GetReport(Guid reportId);
    IEnumerable<NotificationRecord> ListNotifications(Guid instituteId);
}
