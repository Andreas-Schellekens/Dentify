using AutoMapper;
using Dentify.Api.Strategies;
using Dentify.Contracts.Models;
using Dentify.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dentify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlatesController : ControllerBase
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        // Phone originals are often 10+ MB; they are shrunk before they go to Azure
        private const long MaxPhotoSize = 20 * 1024 * 1024;

        private IVehicleRepository _vehicleRepository;
        private IPlateReaderService _plateReaderService;
        private IImageResizeService _imageResizeService;
        private IMapper _mapper;

        public PlatesController(IVehicleRepository vehicleRepository,
            IPlateReaderService plateReaderService, IImageResizeService imageResizeService, IMapper mapper)
        {
            _vehicleRepository = vehicleRepository;
            _plateReaderService = plateReaderService;
            _imageResizeService = imageResizeService;
            _mapper = mapper;
        }

        // Reads the license plate on the photo and looks up the matching vehicle
        [HttpPost("scan")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<PlateScanResultDto>> ScanPlate(IFormFile photo)
        {
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest("Only .jpg, .jpeg and .png photos are allowed.");
            if (photo.Length == 0 || photo.Length > MaxPhotoSize)
                return BadRequest("The photo must be between 1 byte and 20 MB.");

            using var memoryStream = new MemoryStream();
            await photo.CopyToAsync(memoryStream);

            byte[] imageData;
            try
            {
                imageData = _imageResizeService.PrepareForAnalysis(memoryStream.ToArray());
            }
            catch (InvalidDataException)
            {
                return BadRequest("The photo could not be read as an image.");
            }

            var result = new PlateScanResultDto
            {
                RecognizedPlate = await _plateReaderService.ReadPlateAsync(imageData)
            };

            if (!string.IsNullOrWhiteSpace(result.RecognizedPlate))
            {
                var vehicle = await _vehicleRepository.GetVehicleByLicensePlateAsync(result.RecognizedPlate);
                if (vehicle != null)
                    result.Vehicle = _mapper.Map<VehicleListDto>(vehicle);
            }

            return Ok(result);
        }
    }
}
