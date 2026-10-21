using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dentify.Domain.Entities
{
    public class InspectionPhoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Relative URL, e.g. "/uploads/3f2a....jpg". The client prefixes it with the API base URL.
        [Required]
        [MaxLength(500)]
        public string Url { get; set; }

        public DateTime UploadedAt { get; set; }

        // Foreign key + navigation (many side of Inspection)
        public int InspectionId { get; set; }

        [ForeignKey(nameof(InspectionId))]
        public Inspection Inspection { get; set; }

        public InspectionPhoto(string url, DateTime uploadedAt)
        {
            Url = url;
            UploadedAt = uploadedAt;
        }
    }
}
