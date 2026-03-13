using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<UserDto>> SignupAsync(RegisterRequest request);
    Task<ApiResponse<bool>> VerifyEmailAsync(OtpRequest request);
    Task<ApiResponse<bool>> SendVerificationEmailAsync(string email);
    Task<ApiResponse<bool>> SendPasswordResetOtpAsync(string email);
    Task<ApiResponse<bool>> ValidatePasswordResetOtpAsync(OtpRequest request);
    Task<ApiResponse<bool>> ResetPasswordWithOtpAsync(string email, string newPassword);
    Task<ApiResponse<bool>> SendLoginOtpAsync(string emailOrPhone);
    Task<ApiResponse<LoginResponse>> VerifyLoginOtpAsync(OtpRequest request);
    Task<ApiResponse<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId);
    Task<ApiResponse<bool>> UserHasPermissionAsync(Guid userId, string permissionCode);
}
