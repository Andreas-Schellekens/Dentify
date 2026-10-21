using AutoMapper;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;

namespace Dentify.Api.MappingProfiles
{
    public class InspectionPhotoProfile : Profile
    {
        public InspectionPhotoProfile()
        {
            CreateMap<InspectionPhoto, InspectionPhotoListDto>();
        }
    }
}
