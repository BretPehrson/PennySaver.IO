using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PennySaver.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveAndIsEmailVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Account",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Account");
        }
    }
}
