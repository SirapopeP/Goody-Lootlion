using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lootlion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdMemberStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "HouseholdMembers",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "HouseholdMembers");
        }
    }
}
