using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSourcing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyGuardrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyGuardrails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WhatAiMayAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EscalationTriggers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefusalTopics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredDisclaimers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarketRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsHumanStates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyGuardrails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyGuardrails");
        }
    }
}
