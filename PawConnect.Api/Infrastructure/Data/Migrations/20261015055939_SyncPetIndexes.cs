using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawConnect.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPetIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pets_Status",
                table: "Pets");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Status_City",
                table: "Pets",
                columns: new[] { "Status", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Status_CreatedAtUtc",
                table: "Pets",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Status_Species",
                table: "Pets",
                columns: new[] { "Status", "Species" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pets_Status_City",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_Status_CreatedAtUtc",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_Status_Species",
                table: "Pets");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_Status",
                table: "Pets",
                column: "Status");
        }
    }
}
