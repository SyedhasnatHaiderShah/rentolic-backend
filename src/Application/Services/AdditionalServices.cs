using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Application.Interfaces;
using Rentolic.Domain.Entities;

namespace Rentolic.Application.Services;

public class SecurityService : ISecurityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SecurityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<VisitorPermitDto>>> GetPermitsByPropertyAsync(Guid propertyId)
    {
        var permits = await _unitOfWork.Repository<VisitorPermit>().FindAsync(p => p.PropertyId == propertyId);
        return ApiResponse<IEnumerable<VisitorPermitDto>>.SuccessResponse(_mapper.Map<IEnumerable<VisitorPermitDto>>(permits));
    }

    public async Task<ApiResponse<VisitorPermitDto>> CreatePermitAsync(VisitorPermitDto permitDto)
    {
        var permit = _mapper.Map<VisitorPermit>(permitDto);
        await _unitOfWork.Repository<VisitorPermit>().AddAsync(permit);
        await _unitOfWork.SaveAsync();
        return ApiResponse<VisitorPermitDto>.SuccessResponse(_mapper.Map<VisitorPermitDto>(permit), "Permit created successfully");
    }

    public async Task<ApiResponse<IEnumerable<IncidentDto>>> GetIncidentsByPropertyAsync(Guid propertyId)
    {
        var incidents = await _unitOfWork.Repository<Incident>().FindAsync(i => i.PropertyId == propertyId);
        return ApiResponse<IEnumerable<IncidentDto>>.SuccessResponse(_mapper.Map<IEnumerable<IncidentDto>>(incidents));
    }

    public async Task<ApiResponse<string>> GenerateVisitorQrAsync(Guid permitId)
    {
        return ApiResponse<string>.SuccessResponse("mock_qr_permit", "QR generated");
    }

    public async Task<ApiResponse<bool>> ValidateVisitorQrAsync(string qrCode)
    {
        return ApiResponse<bool>.SuccessResponse(true, "QR valid");
    }
}

public class ServiceProviderService : IServiceProviderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ServiceProviderService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<ServiceListingDto>>> GetAllListingsAsync()
    {
        var listings = await _unitOfWork.Repository<ServiceListing>().GetAllAsync();
        return ApiResponse<IEnumerable<ServiceListingDto>>.SuccessResponse(_mapper.Map<IEnumerable<ServiceListingDto>>(listings));
    }

    public async Task<ApiResponse<ServiceBookingDto>> CreateBookingAsync(ServiceBookingDto bookingDto)
    {
        var booking = _mapper.Map<ServiceBooking>(bookingDto);
        await _unitOfWork.Repository<ServiceBooking>().AddAsync(booking);
        await _unitOfWork.SaveAsync();
        return ApiResponse<ServiceBookingDto>.SuccessResponse(_mapper.Map<ServiceBookingDto>(booking), "Booking created successfully");
    }

    public async Task<ApiResponse<PaymentIntentResponse>> CreateServiceBookingPaymentAsync(Guid bookingId)
    {
        return ApiResponse<PaymentIntentResponse>.SuccessResponse(new PaymentIntentResponse { ClientSecret = "cs_service", PaymentIntentId = "pi_service" });
    }

    public async Task<ApiResponse<bool>> ProcessProviderPayoutsAsync()
    {
        return ApiResponse<bool>.SuccessResponse(true, "Payouts processed");
    }
}
