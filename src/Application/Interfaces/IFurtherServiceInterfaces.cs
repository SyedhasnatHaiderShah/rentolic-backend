using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<IEnumerable<NotificationDto>>> GetNotificationsByUserAsync(Guid userId);
    Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid notificationId);
    Task<ApiResponse<bool>> SendEmailAsync(string to, string templateKey, object data);
    Task<ApiResponse<bool>> SendBulkSmsAsync(IEnumerable<string> phoneNumbers, string message);
    Task<ApiResponse<bool>> SendWhatsappAsync(string toNumber, string message);
    Task<ApiResponse<bool>> SendAnnouncementAsync(Guid announcementId, string[] channels);
}

public interface IDocumentService
{
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetDocumentsByPropertyAsync(Guid propertyId);
}

public interface IFacilityService
{
    Task<ApiResponse<IEnumerable<FacilityDto>>> GetFacilitiesByPropertyAsync(Guid propertyId);
    Task<ApiResponse<FacilityBookingDto>> CreateBookingAsync(FacilityBookingDto bookingDto);
    Task<ApiResponse<string>> GenerateFacilityQrAsync(Guid bookingId);
}
