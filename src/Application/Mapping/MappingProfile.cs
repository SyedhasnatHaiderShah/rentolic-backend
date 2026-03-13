using AutoMapper;
using Rentolic.Application.DTOs;
using Rentolic.Domain.Entities;

namespace Rentolic.Application.Mapping;

public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Property, PropertyDto>();
        CreateMap<Unit, UnitDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<Lease, LeaseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<IssueReport, IssueReportDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()));

        CreateMap<VisitorPermit, VisitorPermitDto>();
        CreateMap<VisitorPermitDto, VisitorPermit>();

        CreateMap<Incident, IncidentDto>()
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.Severity.ToString()));

        CreateMap<ServiceListing, ServiceListingDto>();
        CreateMap<ServiceBooking, ServiceBookingDto>();
        CreateMap<ServiceBookingDto, ServiceBooking>();
    }
}
