using Dentify.Domain.Entities;
using Dentify.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Dentify.Infrastructure.Services
{
    public class VehicleRepository : IVehicleRepository
    {
        private DentifyContext _context;

        public VehicleRepository(DentifyContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesAsync(string? searchQuery)
        {
            var collection = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(v =>
                    v.LicensePlate.Contains(searchQuery)
                    || v.Brand.Contains(searchQuery)
                    || v.Model.Contains(searchQuery));
            }

            return await collection
                .OrderBy(v => v.LicensePlate)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleAsync(int vehicleId, VehicleInclude include = VehicleInclude.None)
        {
            var query = _context.Vehicles.AsQueryable();

            query = include switch
            {
                VehicleInclude.Inspections => query.Include(v => v.Inspections.OrderByDescending(i => i.Date))
                                                   .ThenInclude(i => i.Damages)
                                                   .AsSplitQuery(),
                _ => query
            };

            return await query.FirstOrDefaultAsync(v => v.Id == vehicleId);
        }

        public async Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate)
        {
            // OCR output can differ in dashes, spaces and casing, so compare normalized plates
            var normalized = NormalizePlate(licensePlate);

            return await _context.Vehicles.FirstOrDefaultAsync(v =>
                v.LicensePlate.Replace("-", "").Replace(" ", "").ToUpper() == normalized);
        }

        public async Task<bool> VehicleExistsAsync(int vehicleId)
            => await _context.Vehicles.AnyAsync(v => v.Id == vehicleId);

        public async Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeVehicleId = null)
        {
            var normalized = NormalizePlate(licensePlate);

            return await _context.Vehicles.AnyAsync(v =>
                v.LicensePlate.Replace("-", "").Replace(" ", "").ToUpper() == normalized
                && v.Id != excludeVehicleId);
        }

        public async Task AddVehicleAsync(Vehicle vehicle)
            => await _context.Vehicles.AddAsync(vehicle);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() >= 0;

        private static string NormalizePlate(string licensePlate)
            => licensePlate.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }
}
