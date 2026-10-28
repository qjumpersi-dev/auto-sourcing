using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EscalationEmail",
                table: "OrganizationProfiles",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationKey",
                table: "Leads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEscalation = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMessages_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConversationKey",
                table: "Leads",
                column: "ConversationKey");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_LeadId",
                table: "ConversationMessages",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ConversationKey",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "EscalationEmail",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "ConversationKey",
                table: "Leads");
        }
    }
}
