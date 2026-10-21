using AutoMapper;
using Dentify.Api.Strategies;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;

namespace Dentify.Api.MappingProfiles
{
    public class DamageProfile : Profile
    {
        public DamageProfile()
        {
            // Enum -> string (Severity) is converted automatically
            CreateMap<Damage, DamageListDto>();

            // Result of the damage detection strategy -> entity (string -> enum for Severity)
            CreateMap<DamagePrediction, Damage>();
        }
    }
}
