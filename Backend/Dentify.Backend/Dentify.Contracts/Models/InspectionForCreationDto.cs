using System.ComponentModel.DataAnnotations;

namespace Dentify.Contracts.Models
{
    public class InspectionForCreationDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be a valid Id.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [RegularExpression("^(Departure|Return)$", ErrorMessage = "{0} must be 'Departure' or 'Return'.")]
        public string Type { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(100, ErrorMessage = "{0} can have at most {1} characters.")]
        public string InspectorName { get; set; }
    }
}
