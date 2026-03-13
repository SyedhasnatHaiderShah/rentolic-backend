using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface IInspectionService
{
    Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByPropertyAsync(Guid propertyId);
}

public interface IUtilityService
{
    Task<ApiResponse<IEnumerable<UtilityDto>>> GetMetersByUnitAsync(Guid unitId);
}

public interface IMoveWorkflowService
{
    Task<ApiResponse<MoveWorkflowDto>> GetWorkflowByLeaseAsync(Guid leaseId);
}
