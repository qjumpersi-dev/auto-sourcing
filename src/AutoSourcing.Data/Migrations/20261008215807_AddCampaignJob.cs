using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JobId",
                table: "Campaigns",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Campaigns");
        }
    }
}
