namespace Dentify.Api.Strategies
{
    public interface IImageResizeService
    {
        // Returns the photo as an upright JPEG whose longest side is at most the configured maximum.
        // Throws InvalidDataException when the data is not a readable image.
        byte[] PrepareForAnalysis(byte[] imageData);
    }
}
