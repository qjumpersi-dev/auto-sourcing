using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientsAndJobBriefFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdjacentIndustries",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowClientNameInMessages",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AlternativeTitles",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundChecks",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExclusionCriteria",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnCallRequired",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PayCurrency",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayPeriod",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RelocationRequired",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredLicences",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleFamily",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleSummary",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftPattern",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SponsorshipAvailable",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TravelRequirement",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vacancies",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WorkAuthorisationRequired",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_UserId",
                table: "Clients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropColumn(
                name: "AdjacentIndustries",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AllowClientNameInMessages",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AlternativeTitles",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "BackgroundChecks",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExclusionCriteria",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "OnCallRequired",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PayCurrency",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PayPeriod",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RelocationRequired",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RequiredLicences",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RoleFamily",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RoleSummary",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ShiftPattern",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SponsorshipAvailable",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TravelRequirement",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Vacancies",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkAuthorisationRequired",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "Jobs");
        }
    }
}
