using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize(Roles = "PLATFORM_ADMIN")]
public class AdminController : BaseApiController
{
    [HttpGet("audit-logs")]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetAuditLogs() => Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(new List<object>()));

    [HttpGet("blacklist")]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetBlacklist() => Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(new List<object>()));

    [HttpPost("blacklist")]
    public ActionResult<ApiResponse<bool>> AddToBlacklist([FromBody] string nationalId) => Ok(ApiResponse<bool>.SuccessResponse(true, "ID added to blacklist"));
}
