using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Payement.Migrations
{
    /// <inheritdoc />
    public partial class payer_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payer_id",
                table: "WebhookLog",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payer_id",
                table: "WebhookLog");
        }
    }
}
