using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOwnedRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows created without a signed-in user (background campaign runs, the public candidate
            // agent, opt-in/SMS webhooks) were saved with UserId = 0 and then hidden by the per-user
            // filter. Re-own them from the lead they belong to.
            migrationBuilder.Sql(@"
                UPDATE om SET om.UserId = l.UserId
                FROM [OutreachMessages] om INNER JOIN [Leads] l ON l.Id = om.LeadId
                WHERE om.UserId = 0;

                UPDATE le SET le.UserId = l.UserId
                FROM [LeadEmails] le INNER JOIN [Leads] l ON l.Id = le.LeadId
                WHERE le.UserId = 0;

                UPDATE lp SET lp.UserId = l.UserId
                FROM [LeadProfiles] lp INNER JOIN [Leads] l ON l.Id = lp.LeadId
                WHERE lp.UserId = 0;

                UPDATE cm SET cm.UserId = l.UserId
                FROM [ConversationMessages] cm INNER JOIN [Leads] l ON l.Id = cm.LeadId
                WHERE cm.UserId = 0;

                UPDATE cc SET cc.UserId = l.UserId
                FROM [ChannelConsents] cc INNER JOIN [Leads] l ON l.Id = cc.LeadId
                WHERE cc.UserId = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
