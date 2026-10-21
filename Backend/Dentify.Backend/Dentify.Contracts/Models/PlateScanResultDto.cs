namespace Dentify.Contracts.Models
{
    public class PlateScanResultDto
    {
        // Text the OCR read from the photo, null when nothing plate-like was found
        public string? RecognizedPlate { get; set; }

        // Matching vehicle, null when the plate is not in the database
        public VehicleListDto? Vehicle { get; set; }

        public bool IsMatch => Vehicle != null;
    }
}
