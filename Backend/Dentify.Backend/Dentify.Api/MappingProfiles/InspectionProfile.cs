using AutoMapper;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;

namespace Dentify.Api.MappingProfiles
{
    public class InspectionProfile : Profile
    {
        public InspectionProfile()
        {
            // Enum -> string (Type) is converted automatically
            CreateMap<Inspection, InspectionListDto>()
                .ForMember(dest => dest.DamageCount,
                    opt => opt.MapFrom(src => src.Damages.Count));

            // VehicleLicensePlate is flattened from Vehicle.LicensePlate
            CreateMap<Inspection, InspectionDetailDto>();

            // String -> enum (Type) is converted automatically, the date is set by the server
            CreateMap<InspectionForCreationDto, Inspection>()
                .ForCtorParam("date", opt => opt.MapFrom(src => DateTime.Now));

            CreateMap<InspectionForPatchDto, Inspection>();
        }
    }
}
