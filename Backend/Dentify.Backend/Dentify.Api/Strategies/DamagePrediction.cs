namespace Dentify.Api.Strategies
{
    // One damage found on a photo by an IDamageDetectionService
    public class DamagePrediction
    {
        // e.g. "Scratch", "Dent"
        public string Description { get; set; }

        // "Minor", "Moderate" or "Severe"
        public string Severity { get; set; }

        // Area of the photo, e.g. "Bottom right"
        public string Location { get; set; }

        // Probability between 0 and 1
        public double Confidence { get; set; }

        public DamagePrediction(string description, string severity, string location, double confidence)
        {
            Description = description;
            Severity = severity;
            Location = location;
            Confidence = confidence;
        }
    }
}
