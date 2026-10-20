using System.ComponentModel.DataAnnotations;

namespace Dentify.Contracts.Models
{
    public class VehicleForUpdateDto
    {
        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(20, ErrorMessage = "{0} can have at most {1} characters.")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(50, ErrorMessage = "{0} can have at most {1} characters.")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(50, ErrorMessage = "{0} can have at most {1} characters.")]
        public string Model { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(50, ErrorMessage = "{0} can have at most {1} characters.")]
        public string Type { get; set; }
    }
}
