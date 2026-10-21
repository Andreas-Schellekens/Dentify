namespace Dentify.Contracts.Models
{
    public class InspectionListDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }

        // "Departure" or "Return"
        public string Type { get; set; }

        public DateTime Date { get; set; }
        public string InspectorName { get; set; }

        // Relative URL of the cover photo, null when no photos were uploaded yet
        public string? PhotoUrl { get; set; }

        public bool DamageDetected { get; set; }
        public int DamageCount { get; set; }
    }
}
