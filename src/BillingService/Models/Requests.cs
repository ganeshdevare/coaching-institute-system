namespace BillingService.Models;

public sealed record FeePlanRequest(Guid CourseId, string Name, decimal Amount, int Installments);
public sealed record PaymentRequest(Guid StudentId, decimal Amount, string Mode, string IdempotencyKey);
public sealed record MockPaymentWebhook(Guid StudentId, decimal Amount, string ProviderReference, string IdempotencyKey);
public sealed record RefundRequest(Guid PaymentId, decimal Amount, string Reason);
public sealed record ReportRequest(string ReportType);
public sealed record FeeReminderRequest(Guid StudentId);
