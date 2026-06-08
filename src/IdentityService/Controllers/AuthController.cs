using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/identity")]
public sealed class AuthController(IIdentityAppService service) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
        => service.Login(request) is { } result
            ? Ok(ApiResponse<object>.Ok(result, "Login successful"))
            : BadRequest(ApiResponse<object>.Fail("Invalid credentials or inactive account"));

    [HttpPost("refresh")]
    public IActionResult Refresh(RefreshRequest request)
        => service.Refresh(request) is { } result
            ? Ok(ApiResponse<object>.Ok(result, "Token refreshed"))
            : BadRequest(ApiResponse<object>.Fail("Invalid refresh token"));

    [HttpPost("password/change")]
    public IActionResult ChangePassword(ChangePasswordRequest request)
        => User.Identity?.IsAuthenticated == true && service.ChangePassword(request, User.Email())
            ? Ok(ApiResponse<object>.Ok(new { changed = true }))
            : BadRequest(ApiResponse<object>.Fail("Password change failed"));

    [HttpPost("password/reset")]
    public IActionResult ResetPassword(ResetPasswordRequest request)
        => Ok(ApiResponse<object>.Ok(service.ResetPassword(request), "Mock reset token generated"));

    [HttpGet("me")]
    public IActionResult Me()
        => User.Identity?.IsAuthenticated == true
            ? Ok(ApiResponse<object>.Ok(new { email = User.Email(), role = User.Role(), instituteId = User.InstituteId() }))
            : Unauthorized();
}
