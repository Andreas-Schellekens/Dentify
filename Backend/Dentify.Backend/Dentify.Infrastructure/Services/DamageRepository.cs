using Dentify.Domain.Entities;
using Dentify.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Dentify.Infrastructure.Services
{
    public class DamageRepository : IDamageRepository
    {
        private DentifyContext _context;

        public DamageRepository(DentifyContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Damage>> GetDamagesForInspectionAsync(int inspectionId)
        {
            return await _context.Damages
                .Where(d => d.InspectionId == inspectionId)
                .OrderByDescending(d => d.Confidence)
                .ToListAsync();
        }

        public async Task AddDamageAsync(Damage damage)
            => await _context.Damages.AddAsync(damage);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() >= 0;
    }
}
