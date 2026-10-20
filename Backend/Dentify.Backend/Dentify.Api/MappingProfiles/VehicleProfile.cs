using AutoMapper;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;

namespace Dentify.Api.MappingProfiles
{
    public class VehicleProfile : Profile
    {
        public VehicleProfile()
        {
            CreateMap<Vehicle, VehicleListDto>();

            CreateMap<Vehicle, VehicleDetailDto>()
                .ForMember(dest => dest.InspectionCount,
                    opt => opt.MapFrom(src => src.Inspections.Count));

            CreateMap<VehicleForCreationDto, Vehicle>();
            CreateMap<VehicleForUpdateDto, Vehicle>();
        }
    }
}
