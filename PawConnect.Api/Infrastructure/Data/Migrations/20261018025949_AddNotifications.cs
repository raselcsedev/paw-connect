using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawConnect.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedAdoptionSlot",
                table: "Applications",
                newName: "ApprovedSlot");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_PetId_ApprovedAdoptionSlot",
                table: "Applications",
                newName: "IX_Applications_PetId_ApprovedSlot");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "Applications",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "ApprovedSlot",
                table: "Applications",
                newName: "ApprovedAdoptionSlot");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_PetId_ApprovedSlot",
                table: "Applications",
                newName: "IX_Applications_PetId_ApprovedAdoptionSlot");
        }
    }
}
