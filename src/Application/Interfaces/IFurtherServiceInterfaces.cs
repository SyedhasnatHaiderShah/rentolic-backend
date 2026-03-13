using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(Guid userId);
    Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid notificationId);
}

public interface IDocumentService
{
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetDocumentsByPropertyAsync(Guid propertyId);
}

public interface IFacilityService
{
    Task<ApiResponse<IEnumerable<FacilityDto>>> GetFacilitiesByPropertyAsync(Guid propertyId);
    Task<ApiResponse<FacilityBookingDto>> CreateBookingAsync(FacilityBookingDto bookingDto);
}
