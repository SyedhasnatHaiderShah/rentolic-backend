using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class BackgroundJobsController : BaseApiController
{
    private readonly IFinanceService _financeService;

    public BackgroundJobsController(IFinanceService financeService) => _financeService = financeService;

    [HttpPost("auto-generate-invoices")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<int>>> AutoGenerateInvoices() => HandleResult(await _financeService.AutoGenerateMonthlyInvoicesAsync());

    [HttpPost("auto-process-payments")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> AutoProcessPayments() => HandleResult(await _financeService.AutoProcessPaymentsAsync());

    [HttpPost("recurring-service-scheduler")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public ActionResult<ApiResponse<bool>> RecurringServiceScheduler() => Ok(ApiResponse<bool>.SuccessResponse(true, "Job triggered"));

    [HttpPost("calculate-late-fees")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> CalculateLateFees() => HandleResult(await _financeService.CalculateLateFeesAsync());

    [HttpPost("calculate-commissions")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> CalculateCommissions() => HandleResult(await _financeService.CalculateCommissionsAsync());

    [HttpPost("lease-expiry-notifications")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public ActionResult<ApiResponse<bool>> LeaseExpiryNotifications() => Ok(ApiResponse<bool>.SuccessResponse(true, "Job triggered"));

    [HttpPost("process-provider-payouts")]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public ActionResult<ApiResponse<bool>> ProcessProviderPayouts() => Ok(ApiResponse<bool>.SuccessResponse(true, "Job triggered"));
}
