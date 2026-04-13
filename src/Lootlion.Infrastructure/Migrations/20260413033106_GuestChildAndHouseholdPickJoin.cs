using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lootlion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GuestChildAndHouseholdPickJoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowChildPickJoin",
                table: "Households",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuestAccountExpiresUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GuestAccountExpiresUtc",
                table: "AspNetUsers",
                column: "GuestAccountExpiresUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GuestAccountExpiresUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AllowChildPickJoin",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "GuestAccountExpiresUtc",
                table: "AspNetUsers");
        }
    }
}
