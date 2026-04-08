using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blind_Match_PAS.Migrations
{
    public partial class SyncMatchingRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty: The table and columns already exist in the database.
            // We leave this empty to prevent EF from trying to recreate them.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty: No changes to revert.
        }
    }
}