namespace Dentify.Api.Strategies
{
    // What the review did with the damages Custom Vision proposed for one photo.
    // Every proposed damage ends up in exactly one of the three lists.
    public class DamageReviewResult
    {
        // Damages that are stored: confirmed by the AI check, or confident enough when there was no AI verdict
        public List<DamagePrediction> Kept { get; set; }

        // Damages the AI check looked at and found not really there
        public List<DamagePrediction> Rejected { get; set; }

        // Damages without an AI verdict (check unavailable, or past the ones it looks at) and below the threshold
        public List<DamagePrediction> Uncertain { get; set; }

        public DamageReviewResult(List<DamagePrediction> kept, List<DamagePrediction> rejected,
            List<DamagePrediction> uncertain)
        {
            Kept = kept;
            Rejected = rejected;
            Uncertain = uncertain;
        }
    }
}
