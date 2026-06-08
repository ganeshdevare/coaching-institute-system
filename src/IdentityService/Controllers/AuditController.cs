using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity/audit")]
public sealed class AuditController(AppDataStore store) : ControllerBase
{
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? path, [FromQuery] string? correlationId)
    {
        var data = store.AuditLogs.Values
            .Where(x => string.IsNullOrWhiteSpace(path) || x.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(correlationId) || x.CorrelationId == correlationId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(100);

        return Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(data));
    }
}
