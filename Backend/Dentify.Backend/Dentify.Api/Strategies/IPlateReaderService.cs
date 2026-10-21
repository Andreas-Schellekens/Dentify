namespace Dentify.Api.Strategies
{
    public interface IPlateReaderService
    {
        // Returns the license plate text found on the photo, or null when none was found
        Task<string?> ReadPlateAsync(byte[] imageData);
    }
}
