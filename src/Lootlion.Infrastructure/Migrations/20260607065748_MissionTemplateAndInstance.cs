using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lootlion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MissionTemplateAndInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Households",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Asia/Bangkok");

            migrationBuilder.CreateTable(
                name: "MissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RewardExp = table.Column<int>(type: "integer", nullable: false),
                    RewardCoin = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AssignmentMode = table.Column<int>(type: "integer", nullable: false),
                    DefaultAssigneeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurrenceKind = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionTemplates_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AvailableFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionInstances_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MissionInstances_MissionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionInstances_HouseholdId_Status_AssignedToUserId",
                table: "MissionInstances",
                columns: new[] { "HouseholdId", "Status", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionInstances_TemplateId_PeriodKey",
                table: "MissionInstances",
                columns: new[] { "TemplateId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplates_HouseholdId_IsActive",
                table: "MissionTemplates",
                columns: new[] { "HouseholdId", "IsActive" });

            // Migrate legacy Missions -> Template + Instance (same Id for traceability)
            migrationBuilder.Sql("""
                INSERT INTO "MissionTemplates" (
                    "Id", "HouseholdId", "CreatedByUserId", "Title", "Description",
                    "RewardExp", "RewardCoin", "RequiresApproval", "AssignmentMode",
                    "DefaultAssigneeUserId", "RecurrenceKind", "IsActive", "CreatedUtc", "CancelledUtc")
                SELECT
                    m."Id",
                    m."HouseholdId",
                    m."AssignedByUserId",
                    m."Title",
                    m."Description",
                    m."RewardExp",
                    m."RewardCoin",
                    m."RequiresApproval",
                    0,
                    m."AssignedToUserId",
                    0,
                    CASE WHEN m."Status" = 5 THEN FALSE ELSE TRUE END,
                    m."CreatedUtc",
                    CASE WHEN m."Status" = 5 THEN m."CreatedUtc" ELSE NULL END
                FROM "Missions" m;

                INSERT INTO "MissionInstances" (
                    "Id", "TemplateId", "HouseholdId", "AssignedToUserId", "PeriodKey",
                    "Status", "AvailableFromUtc", "SubmittedUtc", "CompletedUtc")
                SELECT
                    m."Id",
                    m."Id",
                    m."HouseholdId",
                    m."AssignedToUserId",
                    'legacy-' || m."Id"::text,
                    CASE m."Status"
                        WHEN 0 THEN 1
                        WHEN 1 THEN 1
                        ELSE m."Status"
                    END,
                    m."CreatedUtc",
                    m."SubmittedUtc",
                    m."CompletedUtc"
                FROM "Missions" m;

                UPDATE "LedgerEntries"
                SET "ReferenceType" = 'MissionInstance'
                WHERE "ReferenceType" = 'Mission';
                """);

            migrationBuilder.DropTable(name: "Missions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RewardCoin = table.Column<int>(type: "integer", nullable: false),
                    RewardExp = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Missions" (
                    "Id", "HouseholdId", "AssignedByUserId", "AssignedToUserId",
                    "Title", "Description", "RewardExp", "RewardCoin", "RequiresApproval",
                    "Status", "CreatedUtc", "SubmittedUtc", "CompletedUtc")
                SELECT
                    i."Id",
                    i."HouseholdId",
                    t."CreatedByUserId",
                    i."AssignedToUserId",
                    t."Title",
                    t."Description",
                    t."RewardExp",
                    t."RewardCoin",
                    t."RequiresApproval",
                    i."Status",
                    t."CreatedUtc",
                    i."SubmittedUtc",
                    i."CompletedUtc"
                FROM "MissionInstances" i
                INNER JOIN "MissionTemplates" t ON t."Id" = i."TemplateId";

                UPDATE "LedgerEntries"
                SET "ReferenceType" = 'Mission'
                WHERE "ReferenceType" = 'MissionInstance';
                """);

            migrationBuilder.DropTable(name: "MissionInstances");
            migrationBuilder.DropTable(name: "MissionTemplates");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Households");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_HouseholdId_Status",
                table: "Missions",
                columns: new[] { "HouseholdId", "Status" });
        }
    }
}
