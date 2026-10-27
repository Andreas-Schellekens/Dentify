namespace Dentify.Maui.Models
{
    // A photo taken or picked on the device, with a preview that does not lock the file
    public class SelectedPhoto
    {
        public FileResult File { get; }
        public ImageSource Preview { get; }

        private SelectedPhoto(FileResult file, ImageSource preview)
        {
            File = file;
            Preview = preview;
        }

        public static async Task<SelectedPhoto> CreateAsync(FileResult file)
        {
            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();
            return new SelectedPhoto(file, ImageSource.FromStream(() => new MemoryStream(imageData)));
        }
    }
}
