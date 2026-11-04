using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoDamageOverlays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelResultUrl",
                table: "InspectionPhotos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewResultUrl",
                table: "InspectionPhotos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelResultUrl",
                table: "InspectionPhotos");

            migrationBuilder.DropColumn(
                name: "ReviewResultUrl",
                table: "InspectionPhotos");
        }
    }
}
