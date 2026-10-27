namespace Dentify.Maui.Services
{
    public interface IPhotoLoaderService
    {
        // Returns null when there is no photo or it could not be loaded, so the UI shows a placeholder
        Task<ImageSource?> LoadAsync(string? relativeUrl);
    }
}
