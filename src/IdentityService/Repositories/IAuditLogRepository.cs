using SharedKernel;

namespace IdentityService.Repositories;

public interface IAuditLogRepository
{
    IEnumerable<AuditLog> Search(string? path, string? correlationId);
}
