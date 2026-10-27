namespace Dentify.Maui.Services
{
    // Wraps MediaPicker. Photos are shrunk on the device, phone originals are too big to upload over Ngrok
    public class PhotoService : IPhotoService
    {
        private const int MaximumPhotoSize = 2048;
        private const int JpegQuality = 85;

        public bool IsCaptureSupported => MediaPicker.Default.IsCaptureSupported;

        public async Task<FileResult?> CapturePhotoAsync()
        {
            if (!IsCaptureSupported)
                throw new PhotoException("This device has no camera. Choose a photo from the gallery instead.");

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                throw new PhotoException("Dentify needs the camera. Allow camera access in the settings of your device.");

            try
            {
                return await MediaPicker.Default.CapturePhotoAsync(CreateOptions("Take a photo"));
            }
            catch (PermissionException exception)
            {
                throw new PhotoException("Dentify needs the camera. Allow camera access in the settings of your device.", exception);
            }
            catch (FeatureNotSupportedException exception)
            {
                throw new PhotoException("The camera is not available on this device.", exception);
            }
        }

        // PickPhotoAsync is obsolete in .NET 10, so a single pick is a multi pick limited to one photo
        public async Task<FileResult?> PickPhotoAsync()
        {
            var photos = await PickPhotosAsync(1);
            return photos.FirstOrDefault();
        }

        public async Task<IEnumerable<FileResult>> PickPhotosAsync(int maximumCount)
        {
            var options = CreateOptions("Choose photos");
            options.SelectionLimit = maximumCount;

            try
            {
                var photos = await MediaPicker.Default.PickPhotosAsync(options);
                return photos?.Where(photo => photo != null).Take(maximumCount).ToList() ?? new List<FileResult>();
            }
            catch (PermissionException exception)
            {
                throw new PhotoException("Dentify needs access to your photos. Allow it in the settings of your device.", exception);
            }
        }

        private static MediaPickerOptions CreateOptions(string title)
        {
            // On Windows the resize drops the EXIF rotation without rotating the pixels, so phone photos
            // arrive sideways. Send the original there: the upload is local and the API rotates and shrinks it.
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
                return new MediaPickerOptions { Title = title };

            return new MediaPickerOptions
            {
                Title = title,
                MaximumWidth = MaximumPhotoSize,
                MaximumHeight = MaximumPhotoSize,
                CompressionQuality = JpegQuality,
                RotateImage = true
            };
        }
    }
}
