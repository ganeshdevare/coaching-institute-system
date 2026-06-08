namespace IdentityService.Models;

public sealed record UserSummaryResponse(Guid Id, Guid? InstituteId, string Email, string DisplayName, string Role, string Status);
