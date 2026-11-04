namespace Dentify.Api.Strategies
{
    public interface IDamageReviewService
    {
        // Gets the damages that Custom Vision proposed for one photo and returns the ones that are really visible.
        // Never adds damages: the trained model decides what to look at.
        Task<IEnumerable<DamagePrediction>> ReviewDamagesAsync(byte[] imageData, IEnumerable<DamagePrediction> candidates);
    }
}
