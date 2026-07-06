using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PennySaver.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidItemId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaidItemId",
                table: "Account",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaidItemId",
                table: "Account");
        }
    }
}
