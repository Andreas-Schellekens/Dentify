using Dentify.Domain.Entities;

namespace Dentify.Infrastructure.Services
{
    public interface IInspectionRepository
    {
        Task<IEnumerable<Inspection>> GetInspectionsForVehicleAsync(int vehicleId);
        Task<Inspection?> GetInspectionAsync(int inspectionId, InspectionInclude include = InspectionInclude.None);
        Task<Inspection?> GetPreviousInspectionAsync(int vehicleId, int currentInspectionId);
        Task<bool> InspectionExistsAsync(int inspectionId);
        Task AddInspectionAsync(Inspection inspection);
        Task<bool> SaveChangesAsync();
    }
}
