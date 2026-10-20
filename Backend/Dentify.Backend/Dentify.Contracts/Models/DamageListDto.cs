namespace Dentify.Contracts.Models
{
    public class DamageListDto
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }
        public string Description { get; set; }

        // "Minor", "Moderate" or "Severe"
        public string Severity { get; set; }

        public string Location { get; set; }
        public double Confidence { get; set; }
    }
}
