using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request) => HandleResult(await _authService.LoginAsync(request));

    [HttpPost("register")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register(RegisterRequest request) => HandleResult(await _authService.RegisterAsync(request));

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserDto>>> Signup(RegisterRequest request) => HandleResult(await _authService.SignupAsync(request));

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail(OtpRequest request) => HandleResult(await _authService.VerifyEmailAsync(request));

    [HttpPost("send-verification-email")]
    public async Task<ActionResult<ApiResponse<bool>>> SendVerificationEmail([FromBody] string email) => HandleResult(await _authService.SendVerificationEmailAsync(email));

    [HttpPost("otp/password-reset/send")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> SendPasswordResetOtp([FromBody] string email) => HandleResult(await _authService.SendPasswordResetOtpAsync(email));

    [HttpPost("otp/password-reset/validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ValidatePasswordResetOtp(OtpRequest request) => HandleResult(await _authService.ValidatePasswordResetOtpAsync(request));

    [HttpPost("otp/password-reset/reset")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(string email, string newPassword) => HandleResult(await _authService.ResetPasswordWithOtpAsync(email, newPassword));

    [HttpPost("otp/login/send")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> SendLoginOtp([FromBody] string emailOrPhone) => HandleResult(await _authService.SendLoginOtpAsync(emailOrPhone));

    [HttpPost("otp/login/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyLoginOtp(OtpRequest request) => HandleResult(await _authService.VerifyLoginOtpAsync(request));

    [HttpPost("demo/reset-passwords")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetDemoPasswords() => Ok(ApiResponse<bool>.SuccessResponse(true, "Demo passwords reset"));

    [HttpPost("demo/reset-password/{userId}")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetDemoPassword(Guid userId) => Ok(ApiResponse<bool>.SuccessResponse(true, $"Demo password reset for user {userId}"));

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUserPermissions()
    {
        // Placeholder for RBAC logic
        return Ok(ApiResponse<IEnumerable<string>>.SuccessResponse(new List<string> { "VIEW_DASHBOARD", "MANAGE_PROPERTIES" }));
    }

    [HttpPost("roles")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> CreateRole([FromBody] string roleName) => Ok(ApiResponse<bool>.SuccessResponse(true, "Role created"));

    [HttpPost("permissions/grant")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> GrantPermission(Guid roleId, Guid permissionId) => Ok(ApiResponse<bool>.SuccessResponse(true, "Permission granted"));
}
