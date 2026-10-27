using Dentify.Contracts.Models;

namespace Dentify.Maui.Services
{
    public interface IApiService
    {
        Task<IEnumerable<VehicleListDto>> GetVehiclesAsync(string? searchQuery = null);
        Task<VehicleDetailDto> GetVehicleAsync(int id);
        Task<IEnumerable<InspectionListDto>> GetInspectionsForVehicleAsync(int vehicleId);
        Task<InspectionDetailDto> GetInspectionAsync(int id);

        Task<PlateScanResultDto> ScanPlateAsync(FileResult photo);
        Task<InspectionDetailDto> CreateInspectionAsync(InspectionForCreationDto inspection);
        Task<InspectionDetailDto> UploadPhotosAsync(int inspectionId, IEnumerable<FileResult> photos);

        // Photos are fetched here too, so the Ngrok header is sent and no other class calls the API
        Task<byte[]> GetPhotoAsync(string relativeUrl);

        Task<bool> CanConnectAsync(string baseUrl);
    }
}
