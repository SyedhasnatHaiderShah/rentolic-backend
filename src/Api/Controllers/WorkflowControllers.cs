using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using System.Security.Claims;

namespace Rentolic.Api.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("sub-user")]
    [Authorize(Roles = "LANDLORD,MAINTENANCE,SECURITY,PROVIDER")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateSubUser(SubUserCreateRequest request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        return HandleResult(await _userService.CreateSubUserAsync(request, role));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        return HandleResult(await _userService.DeleteUserAsync(id));
    }

    [HttpPost("maintenance")]
    [Authorize(Roles = "LANDLORD,PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateMaintenanceUser(SubUserCreateRequest request)
    {
        return HandleResult(await _userService.CreateSubUserAsync(request, "MAINTENANCE"));
    }

    [HttpPost("security")]
    [Authorize(Roles = "LANDLORD,PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateSecurityUser(SubUserCreateRequest request)
    {
        return HandleResult(await _userService.CreateSubUserAsync(request, "SECURITY"));
    }

    [HttpPost("service-provider")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateServiceProviderUser(SubUserCreateRequest request)
    {
        return HandleResult(await _userService.CreateSubUserAsync(request, "PROVIDER"));
    }

    [HttpPost("landlord-sub")]
    [Authorize(Roles = "LANDLORD")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateLandlordSubUser(SubUserCreateRequest request)
    {
        return HandleResult(await _userService.CreateSubUserAsync(request, "LANDLORD"));
    }
}

public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("intent")]
    [Authorize(Roles = "TENANT")]
    public async Task<ActionResult<ApiResponse<PaymentIntentResponse>>> CreateIntent(PaymentIntentRequest request)
    {
        return HandleResult(await _paymentService.CreatePaymentIntentAsync(request));
    }

    [HttpPost("verify")]
    [Authorize(Roles = "LANDLORD,PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyPayment(Guid paymentId, bool verified, string? notes)
    {
        return HandleResult(await _paymentService.VerifyPaymentAsync(paymentId, verified, notes));
    }
}

public class SystemController : BaseApiController
{
    private readonly ISystemTaskService _systemTaskService;

    public SystemController(ISystemTaskService systemTaskService)
    {
        _systemTaskService = systemTaskService;
    }

    [HttpPost("extract-mrz")]
    public async Task<ActionResult<ApiResponse<MrzExtractionResponse>>> ExtractMrz(MrzExtractionRequest request)
    {
        return HandleResult(await _systemTaskService.ExtractMrzDataAsync(request));
    }
}
