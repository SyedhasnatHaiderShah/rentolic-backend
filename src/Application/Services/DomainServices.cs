using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;
using Rentolic.Domain.Enums;

namespace Rentolic.Application.Services;

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PropertyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<PropertyDto>>> GetAllPropertiesAsync()
    {
        var properties = await _unitOfWork.Repository<Property>().GetAllAsync();
        var propertyDtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
        return ApiResponse<IEnumerable<PropertyDto>>.SuccessResponse(propertyDtos);
    }

    public async Task<ApiResponse<PropertyDto>> GetPropertyByIdAsync(Guid id)
    {
        var property = await _unitOfWork.Repository<Property>().GetByIdAsync(id);
        if (property == null) return ApiResponse<PropertyDto>.FailureResponse(new List<string> { "Property not found" });
        return ApiResponse<PropertyDto>.SuccessResponse(_mapper.Map<PropertyDto>(property));
    }

    public async Task<ApiResponse<PropertyDto>> CreatePropertyAsync(PropertyDto propertyDto)
    {
        var property = _mapper.Map<Property>(propertyDto);
        await _unitOfWork.Repository<Property>().AddAsync(property);
        await _unitOfWork.SaveAsync();
        return ApiResponse<PropertyDto>.SuccessResponse(_mapper.Map<PropertyDto>(property), "Property created successfully");
    }
}

public class MaintenanceService : IMaintenanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MaintenanceService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<IssueReportDto>>> GetIssuesByPropertyAsync(Guid propertyId)
    {
        var issues = await _unitOfWork.Repository<IssueReport>().FindAsync(i => i.PropertyId == propertyId);
        return ApiResponse<IEnumerable<IssueReportDto>>.SuccessResponse(_mapper.Map<IEnumerable<IssueReportDto>>(issues));
    }

    public async Task<ApiResponse<IssueReportDto>> CreateIssueReportAsync(IssueReportDto issueReportDto)
    {
        var issue = _mapper.Map<IssueReport>(issueReportDto);
        await _unitOfWork.Repository<IssueReport>().AddAsync(issue);
        await _unitOfWork.SaveAsync();
        return ApiResponse<IssueReportDto>.SuccessResponse(_mapper.Map<IssueReportDto>(issue), "Issue report created successfully");
    }

    public async Task<ApiResponse<bool>> ScheduleWorkAsync(Guid issueId, DateTime scheduledDate)
    {
        var issue = await _unitOfWork.Repository<IssueReport>().GetByIdAsync(issueId);
        if (issue == null) return ApiResponse<bool>.FailureResponse(new List<string> { "Issue not found" });
        issue.ScheduledDate = scheduledDate;
        issue.Status = WorkOrderStatus.IN_PROGRESS;
        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Work scheduled");
    }

    public async Task<ApiResponse<PaymentIntentResponse>> CreateWorkOrderPaymentAsync(Guid issueId)
    {
        return ApiResponse<PaymentIntentResponse>.SuccessResponse(new PaymentIntentResponse { ClientSecret = "cs_work", PaymentIntentId = "pi_work" });
    }
}
