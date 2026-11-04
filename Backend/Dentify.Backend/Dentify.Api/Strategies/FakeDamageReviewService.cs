namespace Dentify.Api.Strategies
{
    // Used without Azure OpenAI: keeps every damage with a confidence of at least "Azure:DamageReview:MinimumProbability"
    public class FakeDamageReviewService : IDamageReviewService
    {
        private double _minimumProbability;

        public FakeDamageReviewService(IConfiguration configuration)
        {
            _minimumProbability = configuration.GetValue("Azure:DamageReview:MinimumProbability", 0.7);
        }

        public Task<IEnumerable<DamagePrediction>> ReviewDamagesAsync(byte[] imageData, IEnumerable<DamagePrediction> candidates)
        {
            IEnumerable<DamagePrediction> kept = candidates
                .Where(c => c.Confidence >= _minimumProbability)
                .ToList();

            return Task.FromResult(kept);
        }
    }
}
