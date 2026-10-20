using System.ComponentModel.DataAnnotations;

namespace Dentify.Contracts.Models
{
    public class InspectionForPatchDto
    {
        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(100, ErrorMessage = "{0} can have at most {1} characters.")]
        public string InspectorName { get; set; }

        [MaxLength(2000, ErrorMessage = "{0} can have at most {1} characters.")]
        public string? AiSummary { get; set; }
    }
}
