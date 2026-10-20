using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dentify.Domain.Enums;

namespace Dentify.Domain.Entities
{
    public class Damage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        public DamageSeverity Severity { get; set; }

        // Part of the vehicle, e.g. "Front bumper", "Left door"
        [Required]
        [MaxLength(100)]
        public string Location { get; set; }

        // Custom Vision probability, between 0 and 1
        [Range(0, 1)]
        public double Confidence { get; set; }

        // Foreign key + navigation (many side of Inspection)
        public int InspectionId { get; set; }

        [ForeignKey(nameof(InspectionId))]
        public Inspection Inspection { get; set; }

        public Damage(string description, DamageSeverity severity, string location, double confidence)
        {
            Description = description;
            Severity = severity;
            Location = location;
            Confidence = confidence;
        }
    }
}
