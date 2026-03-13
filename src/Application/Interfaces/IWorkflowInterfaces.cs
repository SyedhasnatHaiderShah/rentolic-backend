using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> CreateSubUserAsync(SubUserCreateRequest request, string parentRole);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid userId);
}

public interface IPaymentService
{
    Task<ApiResponse<PaymentIntentResponse>> CreatePaymentIntentAsync(PaymentIntentRequest request);
    Task<ApiResponse<bool>> VerifyPaymentAsync(Guid paymentId, bool verified, string? notes);
}

public interface ISystemTaskService
{
    Task<ApiResponse<bool>> SendEmailAsync(string to, string templateKey, object data);
    Task<ApiResponse<MrzExtractionResponse>> ExtractMrzDataAsync(MrzExtractionRequest request);
    Task<ApiResponse<bool>> SendOtpAsync(OtpRequest request, string type);
    Task<ApiResponse<bool>> VerifyOtpAsync(OtpRequest request, string type);
}
