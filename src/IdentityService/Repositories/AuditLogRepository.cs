using SharedKernel;

namespace IdentityService.Repositories;

public sealed class AuditLogRepository : BaseRepository, IAuditLogRepository
{
    public AuditLogRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public IEnumerable<AuditLog> Search(string? path, string? correlationId)
        => store.AuditLogs.Values
            .Where(x => string.IsNullOrWhiteSpace(path) || x.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(correlationId) || x.CorrelationId == correlationId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(100);
}
