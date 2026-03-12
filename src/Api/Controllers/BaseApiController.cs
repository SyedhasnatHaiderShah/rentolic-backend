using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;

namespace Rentolic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> HandleResult<T>(ApiResponse<T> result)
    {
        if (result == null) return NotFound();
        if (result.Success && result.Data != null) return Ok(result);
        if (result.Success && result.Data == null) return NotFound();
        return BadRequest(result);
    }
}
