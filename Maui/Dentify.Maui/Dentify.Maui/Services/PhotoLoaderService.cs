using System.Collections.Concurrent;

namespace Dentify.Maui.Services
{
    // Loads inspection photos through the ApiService and keeps them in memory, stored photos never change
    public class PhotoLoaderService : IPhotoLoaderService
    {
        private IApiService _apiService;
        private ConcurrentDictionary<string, byte[]> _cache = new();

        public PhotoLoaderService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ImageSource?> LoadAsync(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return null;

            try
            {
                if (!_cache.TryGetValue(relativeUrl, out var imageData))
                {
                    imageData = await _apiService.GetPhotoAsync(relativeUrl);
                    _cache[relativeUrl] = imageData;
                }

                return ImageSource.FromStream(() => new MemoryStream(imageData));
            }
            catch (ApiException)
            {
                return null;
            }
        }
    }
}
