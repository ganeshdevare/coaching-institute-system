using SharedKernel;

namespace BillingService.Repositories;

public sealed class BillingRepository : BaseRepository, IBillingRepository
{
    public BillingRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public IEnumerable<FeePlan> ListFeePlans(Guid instituteId)
        => store.FeePlans.Values.Where(x => x.InstituteId == instituteId).OrderBy(x => x.Name);

    public void AddFeePlan(FeePlan feePlan)
        => store.FeePlans[feePlan.Id] = feePlan;

    public IEnumerable<Payment> ListPayments(Guid instituteId, Guid? studentId)
        => store.Payments.Values
            .Where(x => x.InstituteId == instituteId)
            .Where(x => !studentId.HasValue || x.StudentId == studentId)
            .OrderByDescending(x => x.CreatedUtc);

    public Payment? GetPayment(Guid paymentId)
        => store.Payments.GetValueOrDefault(paymentId);

    public Payment? GetPaymentByIdempotencyKey(string idempotencyKey)
        => store.Payments.Values.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey);

    public void AddPayment(Payment payment)
        => store.Payments[payment.Id] = payment;

    public void UpdatePayment(Payment payment)
        => store.Payments[payment.Id] = payment;

    public object BillingHistory(Guid instituteId, Guid studentId)
    {
        var paid = store.Payments.Values.Where(x => x.InstituteId == instituteId && x.StudentId == studentId).ToList();
        var planned = store.FeePlans.Values.Where(x => x.InstituteId == instituteId).Sum(x => x.Amount);
        var netPaid = paid.Sum(x => x.Amount - x.RefundedAmount);
        return new { studentId, planned, netPaid, pending = Math.Max(0, planned - netPaid), creditBalance = Math.Max(0, netPaid - planned), payments = paid };
    }

    public IEnumerable<ReportJob> ListReports(Guid instituteId)
        => store.ReportJobs.Values.Where(x => x.InstituteId == instituteId).OrderByDescending(x => x.CreatedUtc);

    public void AddReport(ReportJob reportJob)
        => store.ReportJobs[reportJob.Id] = reportJob;

    public ReportJob? GetReport(Guid reportId)
        => store.ReportJobs.GetValueOrDefault(reportId);

    public IEnumerable<NotificationRecord> ListNotifications(Guid instituteId)
        => store.Notifications.Values.Where(x => x.InstituteId == instituteId).OrderByDescending(x => x.CreatedUtc);
}
