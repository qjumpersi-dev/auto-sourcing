using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrgName",
                table: "OrganizationProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrgName",
                table: "OrganizationProfiles");
        }
    }
}
