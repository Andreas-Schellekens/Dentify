using AutoMapper;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;
using Dentify.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dentify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectionsController : ControllerBase
    {
        private IInspectionRepository _inspectionRepository;
        private IVehicleRepository _vehicleRepository;
        private IDamageRepository _damageRepository;
        private IMapper _mapper;

        public InspectionsController(IInspectionRepository inspectionRepository,
            IVehicleRepository vehicleRepository, IDamageRepository damageRepository, IMapper mapper)
        {
            _inspectionRepository = inspectionRepository;
            _vehicleRepository = vehicleRepository;
            _damageRepository = damageRepository;
            _mapper = mapper;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InspectionDetailDto>> GetInspection(int id)
        {
            var inspection = await _inspectionRepository.GetInspectionAsync(id, InspectionInclude.VehicleAndDamages);
            if (inspection == null) return NotFound();

            return Ok(_mapper.Map<InspectionDetailDto>(inspection));
        }

        [HttpGet("{inspectionId:int}/damages")]
        public async Task<ActionResult<IEnumerable<DamageListDto>>> GetDamagesForInspection(int inspectionId)
        {
            if (!await _inspectionRepository.InspectionExistsAsync(inspectionId)) return NotFound();

            var damages = await _damageRepository.GetDamagesForInspectionAsync(inspectionId);
            return Ok(_mapper.Map<IEnumerable<DamageListDto>>(damages));
        }

        [HttpPost]
        public async Task<ActionResult<InspectionDetailDto>> CreateInspection(InspectionForCreationDto dto)
        {
            if (!await _vehicleRepository.VehicleExistsAsync(dto.VehicleId))
                return BadRequest($"Vehicle with Id {dto.VehicleId} does not exist.");

            var inspection = _mapper.Map<Inspection>(dto);
            await _inspectionRepository.AddInspectionAsync(inspection);
            await _inspectionRepository.SaveChangesAsync();

            // Reload with the vehicle so the response contains the license plate
            var created = await _inspectionRepository.GetInspectionAsync(inspection.Id, InspectionInclude.VehicleAndDamages);
            var toReturn = _mapper.Map<InspectionDetailDto>(created);
            return CreatedAtAction(nameof(GetInspection), new { id = toReturn.Id }, toReturn);
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> PatchInspection(int id, InspectionForPatchDto dto)
        {
            var inspection = await _inspectionRepository.GetInspectionAsync(id);
            if (inspection == null) return NotFound();

            _mapper.Map(dto, inspection);
            await _inspectionRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
