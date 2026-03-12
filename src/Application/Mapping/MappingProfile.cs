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
    }
}
