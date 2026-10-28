using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class CampaignSequencesAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sequences_Campaigns_CampaignId",
                table: "Sequences");

            migrationBuilder.DropIndex(
                name: "IX_Sequences_CampaignId",
                table: "Sequences");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Sequences");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClickedAt",
                table: "OutreachMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "OutreachMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAt",
                table: "OutreachMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepOrder",
                table: "OutreachMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SequenceId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_SequenceId",
                table: "Campaigns",
                column: "SequenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Sequences_SequenceId",
                table: "Campaigns",
                column: "SequenceId",
                principalTable: "Sequences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Sequences_SequenceId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_SequenceId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ClickedAt",
                table: "OutreachMessages");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "OutreachMessages");

            migrationBuilder.DropColumn(
                name: "RepliedAt",
                table: "OutreachMessages");

            migrationBuilder.DropColumn(
                name: "StepOrder",
                table: "OutreachMessages");

            migrationBuilder.DropColumn(
                name: "SequenceId",
                table: "Campaigns");

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Sequences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sequences_CampaignId",
                table: "Sequences",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sequences_Campaigns_CampaignId",
                table: "Sequences",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
