namespace Dentify.Api.Strategies
{
    // Saves photos in the "uploads" folder of the API. Program.cs serves that folder as static files.
    public class LocalPhotoStorageService : IPhotoStorageService
    {
        public const string FolderName = "uploads";

        private string _uploadPath;

        public LocalPhotoStorageService(IWebHostEnvironment environment)
        {
            _uploadPath = Path.Combine(environment.ContentRootPath, FolderName);
            Directory.CreateDirectory(_uploadPath);
        }

        public async Task<string> SavePhotoAsync(byte[] imageData, string fileExtension)
        {
            var fileName = $"{Guid.NewGuid():N}{fileExtension.ToLowerInvariant()}";
            await File.WriteAllBytesAsync(Path.Combine(_uploadPath, fileName), imageData);

            return $"/{FolderName}/{fileName}";
        }
    }
}
