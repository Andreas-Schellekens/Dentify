using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dentify.Domain.Entities
{
    public class Vehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Brand { get; set; }

        [Required]
        [MaxLength(50)]
        public string Model { get; set; }

        // Kind of vehicle, e.g. "Car", "Van", "Truck"
        [Required]
        [MaxLength(50)]
        public string Type { get; set; }

        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

        public Vehicle(string licensePlate, string brand, string model, string type)
        {
            LicensePlate = licensePlate;
            Brand = brand;
            Model = model;
            Type = type;
        }
    }
}
