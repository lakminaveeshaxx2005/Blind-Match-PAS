using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blind_Match_PAS.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchInterestToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResearchInterest",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResearchInterest",
                table: "AspNetUsers");
        }
    }
}
