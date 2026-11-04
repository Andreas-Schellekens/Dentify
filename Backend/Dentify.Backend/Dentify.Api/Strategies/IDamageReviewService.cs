namespace Dentify.Api.Strategies
{
    public interface IDamageReviewService
    {
        // Gets the damages that Custom Vision proposed for one photo and decides which ones are really visible.
        // Never adds damages: the trained model decides what to look at.
        Task<DamageReviewResult> ReviewDamagesAsync(byte[] imageData, IEnumerable<DamagePrediction> candidates);
    }
}
