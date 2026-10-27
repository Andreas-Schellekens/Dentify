namespace Dentify.Maui.Services
{
    public interface IPhotoService
    {
        bool IsCaptureSupported { get; }

        // Both return null when the user cancels
        Task<FileResult?> CapturePhotoAsync();
        Task<FileResult?> PickPhotoAsync();

        Task<IEnumerable<FileResult>> PickPhotosAsync(int maximumCount);
    }
}
