using Rentolic.Application.DTOs;

namespace Rentolic.Application.Interfaces;

public interface ISecurityService
{
    Task<ApiResponse<IEnumerable<VisitorPermitDto>>> GetPermitsByPropertyAsync(Guid propertyId);
    Task<ApiResponse<VisitorPermitDto>> CreatePermitAsync(VisitorPermitDto permitDto);
    Task<ApiResponse<IEnumerable<IncidentDto>>> GetIncidentsByPropertyAsync(Guid propertyId);
    Task<ApiResponse<string>> GenerateVisitorQrAsync(Guid permitId);
    Task<ApiResponse<bool>> ValidateVisitorQrAsync(string qrCode);
}

public interface IServiceProviderService
{
    Task<ApiResponse<IEnumerable<ServiceListingDto>>> GetAllListingsAsync();
    Task<ApiResponse<ServiceBookingDto>> CreateBookingAsync(ServiceBookingDto bookingDto);
    Task<ApiResponse<PaymentIntentResponse>> CreateServiceBookingPaymentAsync(Guid bookingId);
    Task<ApiResponse<bool>> ProcessProviderPayoutsAsync();
}
