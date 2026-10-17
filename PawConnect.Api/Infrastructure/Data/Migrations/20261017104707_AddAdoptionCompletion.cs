using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawConnect.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedAdoptionSlot",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_PetId_ApprovedAdoptionSlot",
                table: "Applications",
                columns: new[] { "PetId", "ApprovedAdoptionSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_PetId_ApprovedAdoptionSlot",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApprovedAdoptionSlot",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Applications");
        }
    }
}
