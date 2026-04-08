using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blind_Match_PAS.Migrations
{
    public partial class AddMatchingRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Since the table already exists, we skip CreateTable
            // and only add the missing 'ProposalId' column.
            migrationBuilder.AddColumn<int>(
                name: "ProposalId",
                table: "MatchingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // In case of rollback, we only drop the column we added
            migrationBuilder.DropColumn(
                name: "ProposalId",
                table: "MatchingRequests");
        }
    }
}