using Dentify.Domain.Entities;
using Dentify.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Dentify.Infrastructure.Services
{
    public class InspectionPhotoRepository : IInspectionPhotoRepository
    {
        private DentifyContext _context;

        public InspectionPhotoRepository(DentifyContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InspectionPhoto>> GetPhotosForInspectionAsync(int inspectionId)
        {
            return await _context.InspectionPhotos
                .Where(p => p.InspectionId == inspectionId)
                .OrderBy(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task AddPhotoAsync(InspectionPhoto photo)
            => await _context.InspectionPhotos.AddAsync(photo);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() >= 0;
    }
}
