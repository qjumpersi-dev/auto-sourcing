using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SequenceSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Sequences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PolicyGuardrails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "OutreachMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "OrganizationProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LeadProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LeadEmails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ConversationMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ChannelConsents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Existing data predates per-user ownership - hand it to the first user (the admin).
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Users])
                BEGIN
                    DECLARE @owner INT = (SELECT TOP 1 [Id] FROM [Users] ORDER BY [Id]);

                    UPDATE [Campaigns] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [Sequences] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [SequenceSteps] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [Jobs] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [Leads] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [LeadProfiles] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [LeadEmails] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [ChannelConsents] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [ConversationMessages] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [OutreachMessages] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [OrganizationProfiles] SET [UserId] = @owner WHERE [UserId] = 0;
                    UPDATE [PolicyGuardrails] SET [UserId] = @owner WHERE [UserId] = 0;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SequenceSteps_UserId",
                table: "SequenceSteps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sequences_UserId",
                table: "Sequences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyGuardrails_UserId",
                table: "PolicyGuardrails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutreachMessages_UserId",
                table: "OutreachMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProfiles_UserId",
                table: "OrganizationProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_UserId",
                table: "Leads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadProfiles_UserId",
                table: "LeadProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadEmails_UserId",
                table: "LeadEmails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UserId",
                table: "Jobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_UserId",
                table: "ConversationMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConsents_UserId",
                table: "ChannelConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_UserId",
                table: "Campaigns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SequenceSteps_UserId",
                table: "SequenceSteps");

            migrationBuilder.DropIndex(
                name: "IX_Sequences_UserId",
                table: "Sequences");

            migrationBuilder.DropIndex(
                name: "IX_PolicyGuardrails_UserId",
                table: "PolicyGuardrails");

            migrationBuilder.DropIndex(
                name: "IX_OutreachMessages_UserId",
                table: "OutreachMessages");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationProfiles_UserId",
                table: "OrganizationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Leads_UserId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_LeadProfiles_UserId",
                table: "LeadProfiles");

            migrationBuilder.DropIndex(
                name: "IX_LeadEmails_UserId",
                table: "LeadEmails");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_UserId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_UserId",
                table: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChannelConsents_UserId",
                table: "ChannelConsents");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_UserId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SequenceSteps");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Sequences");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PolicyGuardrails");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OutreachMessages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrganizationProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LeadProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LeadEmails");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChannelConsents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Campaigns");
        }
    }
}
