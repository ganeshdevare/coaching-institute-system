using System.Text.Json;

namespace SharedKernel;

public interface IEventPublisher
{
    void Publish<T>(Guid? instituteId, string name, T payload);
}

public sealed class InMemoryEventPublisher(AppDataStore store, IClock clock) : IEventPublisher
{
    public void Publish<T>(Guid? instituteId, string name, T payload)
    {
        store.Events.Enqueue(new DomainEvent(Guid.NewGuid(), instituteId, name, JsonSerializer.Serialize(payload), clock.UtcNow));
    }
}

public static class EventNames
{
    public const string PaymentReceived = "payment.received";
    public const string RefundProcessed = "refund.processed";
    public const string LowAttendanceDetected = "attendance.low_detected";
    public const string FeeReminderRequested = "fees.reminder_requested";
    public const string ReportRequested = "report.requested";
}
