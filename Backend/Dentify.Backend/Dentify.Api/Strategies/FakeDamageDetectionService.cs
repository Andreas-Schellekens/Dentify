namespace Dentify.Api.Strategies
{
    // Development stand-in for Custom Vision: finds one minor scratch on every photo
    public class FakeDamageDetectionService : IDamageDetectionService
    {
        public Task<IEnumerable<DamagePrediction>> DetectDamagesAsync(byte[] imageData)
        {
            IEnumerable<DamagePrediction> predictions = new List<DamagePrediction>
            {
                new DamagePrediction("Scratch", "Minor", "Bottom right", 0.82)
            };

            return Task.FromResult(predictions);
        }
    }
}
