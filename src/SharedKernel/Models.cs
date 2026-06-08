using System.Collections.Concurrent;
using System.Security.Claims;

namespace SharedKernel;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data = default)
{
    public static ApiResponse<T> Ok(T data, string message = "OK") => new(true, message, data);
    public static ApiResponse<T> Fail(string message) => new(false, message);
}

public sealed record TenantContext(Guid? InstituteId, string? Subdomain, bool IsResolved);

public sealed record Institute(Guid Id, string Name, string Subdomain, string OwnerEmail, string Status, DateTime CreatedUtc);
public sealed record Branch(Guid Id, Guid InstituteId, string Name, string Address, bool IsActive);
public sealed record AppUser(Guid Id, Guid? InstituteId, string Email, string PasswordHash, string DisplayName, string Role, string Status);
public sealed record RefreshToken(Guid Id, Guid UserId, string Token, DateTime ExpiresUtc, bool Revoked);
public sealed record Course(Guid Id, Guid InstituteId, string Name, string Code, bool IsActive);
public sealed record Batch(Guid Id, Guid InstituteId, Guid CourseId, Guid? TeacherId, string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive);
public sealed record Enrollment(Guid Id, Guid InstituteId, Guid BatchId, Guid StudentId, string Status);
public sealed record GuardianMap(Guid Id, Guid InstituteId, Guid StudentId, Guid ParentId, string Relationship);
public sealed record FeePlan(Guid Id, Guid InstituteId, Guid CourseId, string Name, decimal Amount, int Installments, bool IsActive);
public sealed record Payment(Guid Id, Guid InstituteId, Guid StudentId, decimal Amount, decimal RefundedAmount, string Mode, string Status, string IdempotencyKey, DateTime CreatedUtc);
public sealed record Attendance(Guid Id, Guid InstituteId, Guid BatchId, Guid StudentId, DateOnly ClassDate, bool IsPresent);
public sealed record NotificationRecord(Guid Id, Guid? InstituteId, string EventName, string Recipient, string Payload, string Status, DateTime CreatedUtc);
public sealed record ReportJob(Guid Id, Guid InstituteId, string ReportType, string Status, string? ResultPath, DateTime CreatedUtc);
public sealed record AuditLog(Guid Id, string CorrelationId, Guid? InstituteId, string Method, string Path, int StatusCode, string UserEmail, DateTime CreatedUtc);
public sealed record DomainEvent(Guid Id, Guid? InstituteId, string Name, string Payload, DateTime CreatedUtc);

public interface IClock { DateTime UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string InstituteOwner = "InstituteOwner";
    public const string InstituteAdmin = "InstituteAdmin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Parent = "Parent";
}

public sealed class AppDataStore
{
    public ConcurrentDictionary<Guid, Institute> Institutes { get; } = new();
    public ConcurrentDictionary<Guid, Branch> Branches { get; } = new();
    public ConcurrentDictionary<Guid, AppUser> Users { get; } = new();
    public ConcurrentDictionary<Guid, RefreshToken> RefreshTokens { get; } = new();
    public ConcurrentDictionary<Guid, Course> Courses { get; } = new();
    public ConcurrentDictionary<Guid, Batch> Batches { get; } = new();
    public ConcurrentDictionary<Guid, Enrollment> Enrollments { get; } = new();
    public ConcurrentDictionary<Guid, GuardianMap> GuardianMaps { get; } = new();
    public ConcurrentDictionary<Guid, FeePlan> FeePlans { get; } = new();
    public ConcurrentDictionary<Guid, Payment> Payments { get; } = new();
    public ConcurrentDictionary<Guid, Attendance> Attendance { get; } = new();
    public ConcurrentDictionary<Guid, NotificationRecord> Notifications { get; } = new();
    public ConcurrentDictionary<Guid, ReportJob> ReportJobs { get; } = new();
    public ConcurrentDictionary<Guid, AuditLog> AuditLogs { get; } = new();
    public ConcurrentQueue<DomainEvent> Events { get; } = new();
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? InstituteId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst("institute_id")?.Value, out var value) ? value : null;

    public static string Role(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    public static string Email(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";
}
