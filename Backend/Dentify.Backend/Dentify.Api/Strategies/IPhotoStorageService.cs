namespace Dentify.Api.Strategies
{
    public interface IPhotoStorageService
    {
        // Stores the photo and returns its relative URL, e.g. "/uploads/3f2a....jpg"
        Task<string> SavePhotoAsync(byte[] imageData, string fileExtension);
    }
}
