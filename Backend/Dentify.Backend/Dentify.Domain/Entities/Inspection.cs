using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dentify.Domain.Enums;

namespace Dentify.Domain.Entities
{
    public class Inspection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public InspectionType Type { get; set; }

        public DateTime Date { get; set; }

        [Required]
        [MaxLength(100)]
        public string InspectorName { get; set; }

        // Cover photo (the first uploaded photo), shown in lists
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public bool DamageDetected { get; set; }

        [MaxLength(2000)]
        public string? AiSummary { get; set; }

        // Foreign key + navigation (many side of Vehicle)
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle Vehicle { get; set; }

        public ICollection<Damage> Damages { get; set; } = new List<Damage>();

        public ICollection<InspectionPhoto> Photos { get; set; } = new List<InspectionPhoto>();

        public Inspection(InspectionType type, DateTime date, string inspectorName)
        {
            Type = type;
            Date = date;
            InspectorName = inspectorName;
        }
    }
}
