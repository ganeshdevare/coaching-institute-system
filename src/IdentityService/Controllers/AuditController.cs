using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using IdentityService.Repositories;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogRepository auditLogRepository;

    public AuditController(IAuditLogRepository auditLogRepository)
    {
        this.auditLogRepository = auditLogRepository;
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? path, [FromQuery] string? correlationId)
    {
        return Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(auditLogRepository.Search(path, correlationId)));
    }
}
