namespace Dentify.Contracts.Models
{
    public class VehicleDetailDto
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public int InspectionCount { get; set; }
        public ICollection<InspectionListDto> Inspections { get; set; } = new List<InspectionListDto>();
    }
}
