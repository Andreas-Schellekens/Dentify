using Dentify.Domain.Entities;
using Dentify.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dentify.Infrastructure.DbContexts
{
    public class DentifyContext : DbContext
    {
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<Damage> Damages { get; set; }
        public DbSet<InspectionPhoto> InspectionPhotos { get; set; }

        public DentifyContext(DbContextOptions<DentifyContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seeding: "one" side first, then the "many" side. IDs always explicit.
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle("1-ABC-123", "Volkswagen", "Golf", "Car") { Id = 1 },
                new Vehicle("2-DEF-456", "Renault", "Trafic", "Van") { Id = 2 },
                new Vehicle("1-GHI-789", "Toyota", "Yaris", "Car") { Id = 3 }
            );

            modelBuilder.Entity<Inspection>().HasData(
                new Inspection(InspectionType.Departure, new DateTime(2026, 10, 1, 9, 0, 0), "Andreas")
                {
                    Id = 1,
                    VehicleId = 1,
                    DamageDetected = false,
                    AiSummary = "No visible damage detected at departure."
                },
                new Inspection(InspectionType.Return, new DateTime(2026, 10, 5, 17, 30, 0), "Andreas")
                {
                    Id = 2,
                    VehicleId = 1,
                    DamageDetected = true,
                    AiSummary = "A new scratch was found on the front bumper compared to the departure inspection."
                }
            );

            modelBuilder.Entity<Damage>().HasData(
                new Damage("Scratch on the paint", DamageSeverity.Minor, "Front bumper", 0.87) { Id = 1, InspectionId = 2 }
            );
        }
    }
}
