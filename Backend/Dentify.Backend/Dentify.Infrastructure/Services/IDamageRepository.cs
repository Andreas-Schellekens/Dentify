using Dentify.Domain.Entities;

namespace Dentify.Infrastructure.Services
{
    public interface IDamageRepository
    {
        Task<IEnumerable<Damage>> GetDamagesForInspectionAsync(int inspectionId);
        Task AddDamageAsync(Damage damage);
        Task<bool> SaveChangesAsync();
    }
}
