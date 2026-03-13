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

    public async Task<ApiResponse<decimal>> CalculateAssignmentScoreAsync(Guid issueId, Guid teamId)
    {
        var issue = await _unitOfWork.Repository<IssueReport>().GetByIdAsync(issueId);
        var team = await _unitOfWork.Repository<MaintenanceTeam>().GetByIdAsync(teamId);

        if (issue == null || team == null) return ApiResponse<decimal>.FailureResponse(new List<string> { "Issue or Team not found" });

        decimal score = 0;

        // Factor 1: Current workload (0-30 points)
        // ⚡ Bolt: Use CountAsync to get count directly in SQL instead of materializing list
        var activeCount = await _unitOfWork.Repository<IssueReport>().CountAsync(i => i.AssignedMaintenanceTeamId == teamId && i.Status != WorkOrderStatus.COMPLETED && i.Status != WorkOrderStatus.CANCELLED);
        score += Math.Max(0, 30 - (activeCount * 5));

        // Factor 2: Specialty match (0-25 points)
        if (team.Specialties != null && team.Specialties.Contains(issue.Category)) score += 25;

        // Factor 3: Average completion time (0-20 points) - Mocked logic
        score += 15;

        // Factor 4: Team rating (0-25 points) - Mocked logic
        score += 20;

        return ApiResponse<decimal>.SuccessResponse(score);
    }

    public async Task<ApiResponse<bool>> ApplyWorkOrderBusinessRulesAsync(Guid issueId)
    {
        var issue = await _unitOfWork.Repository<IssueReport>().GetByIdAsync(issueId);
        if (issue == null) return ApiResponse<bool>.FailureResponse(new List<string> { "Issue not found" });

        // Set SLA based on priority
        int slaHours = issue.Priority switch
        {
            Priority.EMERGENCY => 4,
            Priority.HIGH => 24,
            Priority.MEDIUM => 72,
            Priority.LOW => 168,
            _ => 72
        };
        issue.SlaDueDate = issue.CreatedAt.AddHours(slaHours);

        // Set approval status if cost estimate > 1000
        if (issue.CostEstimate > 1000) issue.ApprovalStatus = "PENDING";
        else issue.ApprovalStatus = "NOT_REQUIRED";

        await _unitOfWork.SaveAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Business rules applied");
    }
}
