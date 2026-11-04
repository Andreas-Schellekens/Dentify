namespace Dentify.Api.Strategies
{
    // Used without Azure OpenAI: keeps every damage with a confidence of at least "Azure:DamageReview:MinimumProbability".
    // Nothing is checked on the photo, so nothing is "rejected": the rest is only too uncertain to keep.
    public class FakeDamageReviewService : IDamageReviewService
    {
        private double _minimumProbability;

        public FakeDamageReviewService(IConfiguration configuration)
        {
            _minimumProbability = configuration.GetValue("Azure:DamageReview:MinimumProbability", 0.7);
        }

        public Task<DamageReviewResult> ReviewDamagesAsync(byte[] imageData, IEnumerable<DamagePrediction> candidates)
        {
            var list = candidates.ToList();
            var kept = list.Where(c => c.Confidence >= _minimumProbability).ToList();
            var uncertain = list.Except(kept).ToList();

            return Task.FromResult(new DamageReviewResult(kept, new List<DamagePrediction>(), uncertain));
        }
    }
}
