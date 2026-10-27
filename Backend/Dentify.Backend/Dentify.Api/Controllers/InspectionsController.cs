using AutoMapper;
using Dentify.Api.Strategies;
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
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxPhotoSize = 10 * 1024 * 1024;
        private const int MaxPhotosPerUpload = 8;

        private IInspectionRepository _inspectionRepository;
        private IVehicleRepository _vehicleRepository;
        private IDamageRepository _damageRepository;
        private IInspectionPhotoRepository _photoRepository;
        private IPhotoStorageService _photoStorageService;
        private IDamageDetectionService _damageDetectionService;
        private IReportService _reportService;
        private IMapper _mapper;

        public InspectionsController(IInspectionRepository inspectionRepository,
            IVehicleRepository vehicleRepository, IDamageRepository damageRepository,
            IInspectionPhotoRepository photoRepository, IPhotoStorageService photoStorageService,
            IDamageDetectionService damageDetectionService, IReportService reportService, IMapper mapper)
        {
            _inspectionRepository = inspectionRepository;
            _vehicleRepository = vehicleRepository;
            _damageRepository = damageRepository;
            _photoRepository = photoRepository;
            _photoStorageService = photoStorageService;
            _damageDetectionService = damageDetectionService;
            _reportService = reportService;
            _mapper = mapper;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InspectionDetailDto>> GetInspection(int id)
        {
            var inspection = await _inspectionRepository.GetInspectionAsync(id, InspectionInclude.All);
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
            var created = await _inspectionRepository.GetInspectionAsync(inspection.Id, InspectionInclude.All);
            var toReturn = _mapper.Map<InspectionDetailDto>(created);
            return CreatedAtAction(nameof(GetInspection), new { id = toReturn.Id }, toReturn);
        }

        // Stores the photos, runs damage detection on each one and writes the AI summary
        [HttpPost("{id:int}/photos")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxPhotosPerUpload * MaxPhotoSize + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxPhotosPerUpload * MaxPhotoSize + 1024 * 1024)]
        public async Task<ActionResult<InspectionDetailDto>> UploadPhotos(int id, List<IFormFile> photos)
        {
            var inspection = await _inspectionRepository.GetInspectionAsync(id);
            if (inspection == null) return NotFound();

            if (photos == null || photos.Count == 0)
                return BadRequest("Upload at least one photo.");
            if (photos.Count > MaxPhotosPerUpload)
                return BadRequest($"Upload at most {MaxPhotosPerUpload} photos at once.");

            foreach (var photo in photos)
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(photo.FileName).ToLowerInvariant()))
                    return BadRequest($"{photo.FileName}: only .jpg, .jpeg and .png photos are allowed.");
                if (photo.Length == 0 || photo.Length > MaxPhotoSize)
                    return BadRequest($"{photo.FileName}: the photo must be between 1 byte and 10 MB.");
            }

            // Run damage detection on all photos first, so nothing is stored when the AI service is unavailable
            var analyzedPhotos = new List<(byte[] ImageData, string Extension, IEnumerable<DamagePrediction> Predictions)>();
            foreach (var photo in photos)
            {
                using var memoryStream = new MemoryStream();
                await photo.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                try
                {
                    var predictions = await _damageDetectionService.DetectDamagesAsync(imageData);
                    analyzedPhotos.Add((imageData, Path.GetExtension(photo.FileName), predictions));
                }
                catch (Exception exception) when (exception is HttpRequestException or Azure.RequestFailedException)
                {
                    return StatusCode(StatusCodes.Status502BadGateway,
                        $"Damage detection is not available right now: {exception.Message}");
                }
            }

            foreach (var (imageData, extension, predictions) in analyzedPhotos)
            {
                var url = await _photoStorageService.SavePhotoAsync(imageData, extension);
                await _photoRepository.AddPhotoAsync(new InspectionPhoto(url, DateTime.Now) { InspectionId = id });
                inspection.PhotoUrl ??= url;

                foreach (var prediction in predictions)
                {
                    var damage = _mapper.Map<Damage>(prediction);
                    damage.InspectionId = id;
                    await _damageRepository.AddDamageAsync(damage);
                }
            }

            await _inspectionRepository.SaveChangesAsync();

            // Summary over all damages of this inspection, compared with the previous inspection of the vehicle
            var updated = await _inspectionRepository.GetInspectionAsync(id, InspectionInclude.All);
            var toReturn = _mapper.Map<InspectionDetailDto>(updated);

            var previous = await _inspectionRepository.GetPreviousInspectionAsync(updated.VehicleId, id);
            var previousDto = previous == null ? null : _mapper.Map<InspectionDetailDto>(previous);

            updated.DamageDetected = updated.Damages.Any();
            updated.AiSummary = await _reportService.WriteSummaryAsync(toReturn, previousDto);
            await _inspectionRepository.SaveChangesAsync();

            toReturn.DamageDetected = updated.DamageDetected;
            toReturn.AiSummary = updated.AiSummary;
            return CreatedAtAction(nameof(GetInspection), new { id }, toReturn);
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
