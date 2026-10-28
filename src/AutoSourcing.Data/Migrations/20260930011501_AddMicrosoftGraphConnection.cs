using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMicrosoftGraphConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MicrosoftAccessToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MicrosoftAccessTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftAccountEmail",
                table: "Users",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MicrosoftConnectedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftRefreshToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MicrosoftAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MicrosoftAccessTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MicrosoftAccountEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MicrosoftConnectedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MicrosoftRefreshToken",
                table: "Users");
        }
    }
}
