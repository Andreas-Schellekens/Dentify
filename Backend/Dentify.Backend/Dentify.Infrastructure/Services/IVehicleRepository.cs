using Dentify.Domain.Entities;

namespace Dentify.Infrastructure.Services
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<Vehicle>> GetVehiclesAsync(string? searchQuery);
        Task<Vehicle?> GetVehicleAsync(int vehicleId, VehicleInclude include = VehicleInclude.None);
        Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate);
        Task<bool> VehicleExistsAsync(int vehicleId);
        Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeVehicleId = null);
        Task AddVehicleAsync(Vehicle vehicle);
        Task<bool> SaveChangesAsync();
    }
}
