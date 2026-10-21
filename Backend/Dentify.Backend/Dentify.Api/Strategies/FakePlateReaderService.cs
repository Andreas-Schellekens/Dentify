namespace Dentify.Api.Strategies
{
    // Development stand-in for Azure AI Vision: always "reads" the plate of seeded vehicle 1
    public class FakePlateReaderService : IPlateReaderService
    {
        public Task<string?> ReadPlateAsync(byte[] imageData)
        {
            return Task.FromResult<string?>("1-ABC-123");
        }
    }
}
