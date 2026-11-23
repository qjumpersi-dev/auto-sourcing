using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentPoolTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndustryTag",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitleTag",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicencesHeld",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticePeriod",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayExpectation",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleFamilyTag",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceIndustry",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkAuthorisation",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "IndustryTag",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "JobTitleTag",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LicencesHeld",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "NoticePeriod",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "PayExpectation",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "RoleFamilyTag",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SourceIndustry",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "WorkAuthorisation",
                table: "Leads");
        }
    }
}
