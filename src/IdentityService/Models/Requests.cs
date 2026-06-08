namespace IdentityService.Models;

public sealed record RegisterInstituteRequest(string Name, string Subdomain, string OwnerEmail, string OwnerName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record InviteUserRequest(string Email, string DisplayName, string Role);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ResetPasswordRequest(string Email);
