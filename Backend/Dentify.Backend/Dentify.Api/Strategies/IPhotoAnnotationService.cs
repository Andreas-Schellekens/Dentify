namespace Dentify.Api.Strategies
{
    public interface IPhotoAnnotationService
    {
        // Draws a box with a label around every damage the trained model proposed. Returns a JPEG.
        byte[] DrawDetections(byte[] imageData, IEnumerable<DamagePrediction> damages);

        // Draws the confirmed damages and, crossed out, the ones the review rejected. Returns a JPEG.
        byte[] DrawReview(byte[] imageData, IEnumerable<DamagePrediction> confirmed, IEnumerable<DamagePrediction> rejected);
    }
}
