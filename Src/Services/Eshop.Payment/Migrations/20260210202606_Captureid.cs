using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Payment.Migrations
{
    /// <inheritdoc />
    public partial class Captureid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaptureId",
                table: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptureId",
                table: "Payments");
        }
    }
}
