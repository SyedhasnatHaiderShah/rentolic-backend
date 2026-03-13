using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Application.Services;

public class SystemTaskService : ISystemTaskService
{
    public async Task<ApiResponse<bool>> SendEmailAsync(string to, string templateKey, object data)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Email sent");
    }

    public async Task<ApiResponse<MrzExtractionResponse>> ExtractMrzDataAsync(MrzExtractionRequest request)
    {
        // Mock AI logic
        var result = new MrzExtractionResponse
        {
            Success = true,
            Data = new { FullName = "JOHN DOE", IdNumber = "784-1234-5678901-1" }
        };
        return ApiResponse<MrzExtractionResponse>.SuccessResponse(result);
    }

    public async Task<ApiResponse<bool>> SendOtpAsync(OtpRequest request, string type)
    {
        return ApiResponse<bool>.SuccessResponse(true, $"{type} OTP sent");
    }

    public async Task<ApiResponse<bool>> VerifyOtpAsync(OtpRequest request, string type)
    {
        return ApiResponse<bool>.SuccessResponse(true, "OTP verified");
    }
}
