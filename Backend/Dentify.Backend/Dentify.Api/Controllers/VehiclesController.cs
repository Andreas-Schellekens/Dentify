using AutoMapper;
using Dentify.Contracts.Models;
using Dentify.Domain.Entities;
using Dentify.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dentify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private IVehicleRepository _vehicleRepository;
        private IInspectionRepository _inspectionRepository;
        private IMapper _mapper;

        public VehiclesController(IVehicleRepository vehicleRepository,
            IInspectionRepository inspectionRepository, IMapper mapper)
        {
            _vehicleRepository = vehicleRepository;
            _inspectionRepository = inspectionRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleListDto>>> GetVehicles(string? searchQuery)
        {
            var vehicles = await _vehicleRepository.GetVehiclesAsync(searchQuery);
            return Ok(_mapper.Map<IEnumerable<VehicleListDto>>(vehicles));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VehicleDetailDto>> GetVehicle(int id)
        {
            var vehicle = await _vehicleRepository.GetVehicleAsync(id, VehicleInclude.Inspections);
            if (vehicle == null) return NotFound();

            return Ok(_mapper.Map<VehicleDetailDto>(vehicle));
        }

        [HttpGet("by-plate/{licensePlate}")]
        public async Task<ActionResult<VehicleListDto>> GetVehicleByLicensePlate(string licensePlate)
        {
            var vehicle = await _vehicleRepository.GetVehicleByLicensePlateAsync(licensePlate);
            if (vehicle == null) return NotFound();

            return Ok(_mapper.Map<VehicleListDto>(vehicle));
        }

        [HttpGet("{vehicleId:int}/inspections")]
        public async Task<ActionResult<IEnumerable<InspectionListDto>>> GetInspectionsForVehicle(int vehicleId)
        {
            if (!await _vehicleRepository.VehicleExistsAsync(vehicleId)) return NotFound();

            var inspections = await _inspectionRepository.GetInspectionsForVehicleAsync(vehicleId);
            return Ok(_mapper.Map<IEnumerable<InspectionListDto>>(inspections));
        }

        [HttpPost]
        public async Task<ActionResult<VehicleDetailDto>> CreateVehicle(VehicleForCreationDto dto)
        {
            if (await _vehicleRepository.LicensePlateExistsAsync(dto.LicensePlate))
                return Conflict($"A vehicle with license plate {dto.LicensePlate} already exists.");

            var vehicle = _mapper.Map<Vehicle>(dto);
            await _vehicleRepository.AddVehicleAsync(vehicle);
            await _vehicleRepository.SaveChangesAsync();

            var toReturn = _mapper.Map<VehicleDetailDto>(vehicle);
            return CreatedAtAction(nameof(GetVehicle), new { id = toReturn.Id }, toReturn);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateVehicle(int id, VehicleForUpdateDto dto)
        {
            var vehicle = await _vehicleRepository.GetVehicleAsync(id);
            if (vehicle == null) return NotFound();

            if (await _vehicleRepository.LicensePlateExistsAsync(dto.LicensePlate, id))
                return Conflict($"A vehicle with license plate {dto.LicensePlate} already exists.");

            _mapper.Map(dto, vehicle);
            await _vehicleRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
