using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;

namespace Rentolic.Api.Controllers;

[Authorize]
public class InspectionsController : BaseApiController
{
    private readonly IInspectionService _inspectionService;

    public InspectionsController(IInspectionService inspectionService) => _inspectionService = inspectionService;

    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InspectionDto>>>> GetInspections(Guid propertyId) => HandleResult(await _inspectionService.GetInspectionsByPropertyAsync(propertyId));
}

public class UtilitiesController : BaseApiController
{
    private readonly IUtilityService _utilityService;

    public UtilitiesController(IUtilityService utilityService) => _utilityService = utilityService;

    [HttpGet("unit/{unitId}/meters")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UtilityDto>>>> GetMeters(Guid unitId) => HandleResult(await _utilityService.GetMetersByUnitAsync(unitId));
}
