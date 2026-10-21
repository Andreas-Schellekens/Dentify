using Dentify.Domain.Entities;

namespace Dentify.Infrastructure.Services
{
    public interface IInspectionPhotoRepository
    {
        Task<IEnumerable<InspectionPhoto>> GetPhotosForInspectionAsync(int inspectionId);
        Task AddPhotoAsync(InspectionPhoto photo);
        Task<bool> SaveChangesAsync();
    }
}
