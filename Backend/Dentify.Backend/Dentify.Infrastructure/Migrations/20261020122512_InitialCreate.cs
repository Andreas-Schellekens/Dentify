using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dentify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DamageDetected = table.Column<bool>(type: "bit", nullable: false),
                    AiSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Damages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    InspectionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Damages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Damages_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Brand", "LicensePlate", "Model", "Type" },
                values: new object[,]
                {
                    { 1, "Volkswagen", "1-ABC-123", "Golf", "Car" },
                    { 2, "Renault", "2-DEF-456", "Trafic", "Van" },
                    { 3, "Toyota", "1-GHI-789", "Yaris", "Car" }
                });

            migrationBuilder.InsertData(
                table: "Inspections",
                columns: new[] { "Id", "AiSummary", "DamageDetected", "Date", "InspectorName", "PhotoUrl", "Type", "VehicleId" },
                values: new object[,]
                {
                    { 1, "No visible damage detected at departure.", false, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Andreas", null, 0, 1 },
                    { 2, "A new scratch was found on the front bumper compared to the departure inspection.", true, new DateTime(2026, 10, 5, 17, 30, 0, 0, DateTimeKind.Unspecified), "Andreas", null, 1, 1 }
                });

            migrationBuilder.InsertData(
                table: "Damages",
                columns: new[] { "Id", "Confidence", "Description", "InspectionId", "Location", "Severity" },
                values: new object[] { 1, 0.87, "Scratch on the paint", 2, "Front bumper", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Damages_InspectionId",
                table: "Damages",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_VehicleId",
                table: "Inspections",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Damages");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
