namespace Dentify.Api.Strategies
{
    public interface IDamageDetectionService
    {
        Task<IEnumerable<DamagePrediction>> DetectDamagesAsync(byte[] imageData);
    }
}
