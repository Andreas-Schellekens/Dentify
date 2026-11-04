namespace Dentify.Api.Strategies
{
    public interface IPhotoAnnotationService
    {
        // Draws a box with a label around every damage the trained model proposed. Returns a JPEG.
        byte[] DrawDetections(byte[] imageData, IEnumerable<DamagePrediction> damages);

        // Draws the kept damages, and dashed the ones the AI check rejected or that were too uncertain. Returns a JPEG.
        byte[] DrawReview(byte[] imageData, DamageReviewResult review);
    }
}
