using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;

namespace Rentolic.Application.Services;

public class FacilityService : IFacilityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FacilityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<FacilityDto>>> GetFacilitiesByPropertyAsync(Guid propertyId)
    {
        var facilities = await _unitOfWork.Repository<Facility>().FindAsync(f => f.PropertyId == propertyId);
        return ApiResponse<IEnumerable<FacilityDto>>.SuccessResponse(_mapper.Map<IEnumerable<FacilityDto>>(facilities));
    }

    public async Task<ApiResponse<FacilityBookingDto>> CreateBookingAsync(FacilityBookingDto bookingDto)
    {
        var booking = _mapper.Map<FacilityBooking>(bookingDto);
        await _unitOfWork.Repository<FacilityBooking>().AddAsync(booking);
        await _unitOfWork.SaveAsync();
        return ApiResponse<FacilityBookingDto>.SuccessResponse(_mapper.Map<FacilityBookingDto>(booking), "Facility booked successfully");
    }

    public async Task<ApiResponse<string>> GenerateFacilityQrAsync(Guid bookingId)
    {
        return ApiResponse<string>.SuccessResponse("mock_qr_facility", "QR generated");
    }
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(Guid userId)
    {
        var notifications = await _unitOfWork.Repository<Notification>().FindAsync(n => n.UserId == userId);
        return ApiResponse<IEnumerable<NotificationDto>>.SuccessResponse(_mapper.Map<IEnumerable<NotificationDto>>(notifications));
    }

    public async Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId);
        if (notification == null) return ApiResponse<NotificationDto>.FailureResponse(new List<string> { "Notification not found" });
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveAsync();
        return ApiResponse<NotificationDto>.SuccessResponse(_mapper.Map<NotificationDto>(notification));
    }

    public async Task<ApiResponse<bool>> SendEmailAsync(string to, string templateKey, object data)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Email sent");
    }

    public async Task<ApiResponse<bool>> SendBulkSmsAsync(IEnumerable<string> phoneNumbers, string message)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Bulk SMS sent");
    }

    public async Task<ApiResponse<bool>> SendWhatsappAsync(string toNumber, string message)
    {
        return ApiResponse<bool>.SuccessResponse(true, "WhatsApp message sent");
    }

    public async Task<ApiResponse<bool>> SendAnnouncementAsync(Guid announcementId, string[] channels)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Announcement sent across channels");
    }
}

public class SmartHomeService : ISmartHomeService
{
    public async Task<ApiResponse<bool>> ProcessVoiceCommandAsync(string command)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Voice command processed");
    }

    public async Task<ApiResponse<bool>> HandleDeviceAlertAsync(Guid deviceId, string alertType)
    {
        return ApiResponse<bool>.SuccessResponse(true, "Device alert handled");
    }
}

public class CommunityService : ICommunityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CommunityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<CommunityChannelDto>>> GetChannelsByPropertyAsync(Guid propertyId)
    {
        var channels = await _unitOfWork.Repository<CommunityChannel>().FindAsync(c => c.PropertyId == propertyId);
        return ApiResponse<IEnumerable<CommunityChannelDto>>.SuccessResponse(_mapper.Map<IEnumerable<CommunityChannelDto>>(channels));
    }
}

public class InspectionService : IInspectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InspectionService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByPropertyAsync(Guid propertyId)
    {
        var inspections = await _unitOfWork.Repository<Inspection>().FindAsync(i => i.PropertyId == propertyId);
        return ApiResponse<IEnumerable<InspectionDto>>.SuccessResponse(_mapper.Map<IEnumerable<InspectionDto>>(inspections));
    }
}

public class UtilityService : IUtilityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UtilityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<UtilityDto>>> GetMetersByUnitAsync(Guid unitId)
    {
        var meters = await _unitOfWork.Repository<UtilityMeter>().FindAsync(m => m.UnitId == unitId);
        return ApiResponse<IEnumerable<UtilityDto>>.SuccessResponse(_mapper.Map<IEnumerable<UtilityDto>>(meters));
    }
}
