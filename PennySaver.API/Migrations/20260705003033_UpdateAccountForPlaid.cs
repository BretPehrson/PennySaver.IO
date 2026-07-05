using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PennySaver.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountForPlaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomated",
                table: "Account",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaidAccessToken",
                table: "Account",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaidAccountId",
                table: "Account",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomated",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "PlaidAccessToken",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "PlaidAccountId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Account");
        }
    }
}
