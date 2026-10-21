using Dentify.Domain.Entities;
using Dentify.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Dentify.Infrastructure.Services
{
    public class InspectionRepository : IInspectionRepository
    {
        private DentifyContext _context;

        public InspectionRepository(DentifyContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Inspection>> GetInspectionsForVehicleAsync(int vehicleId)
        {
            return await _context.Inspections
                .Include(i => i.Damages)
                .Where(i => i.VehicleId == vehicleId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
        }

        public async Task<Inspection?> GetInspectionAsync(int inspectionId, InspectionInclude include = InspectionInclude.None)
        {
            var query = _context.Inspections.AsQueryable();

            query = include switch
            {
                InspectionInclude.Vehicle => query.Include(i => i.Vehicle),
                InspectionInclude.Damages => query.Include(i => i.Damages),
                InspectionInclude.All => query.Include(i => i.Vehicle)
                                              .Include(i => i.Damages)
                                              .Include(i => i.Photos.OrderBy(p => p.UploadedAt))
                                              .AsSplitQuery(),
                _ => query
            };

            return await query.FirstOrDefaultAsync(i => i.Id == inspectionId);
        }

        // Most recent inspection of the same vehicle before the given one, used for the AI comparison
        public async Task<Inspection?> GetPreviousInspectionAsync(int vehicleId, int currentInspectionId)
        {
            var current = await _context.Inspections.FirstOrDefaultAsync(i => i.Id == currentInspectionId);
            if (current == null) return null;

            return await _context.Inspections
                .Include(i => i.Damages)
                .Where(i => i.VehicleId == vehicleId && i.Id != currentInspectionId && i.Date <= current.Date)
                .OrderByDescending(i => i.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> InspectionExistsAsync(int inspectionId)
            => await _context.Inspections.AnyAsync(i => i.Id == inspectionId);

        public async Task AddInspectionAsync(Inspection inspection)
            => await _context.Inspections.AddAsync(inspection);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() >= 0;
    }
}
