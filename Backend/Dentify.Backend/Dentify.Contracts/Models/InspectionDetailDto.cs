namespace Dentify.Contracts.Models
{
    public class InspectionDetailDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleLicensePlate { get; set; }

        // "Departure" or "Return"
        public string Type { get; set; }

        public DateTime Date { get; set; }
        public string InspectorName { get; set; }
        public string? PhotoUrl { get; set; }
        public bool DamageDetected { get; set; }
        public string? AiSummary { get; set; }
        public ICollection<DamageListDto> Damages { get; set; } = new List<DamageListDto>();
    }
}
