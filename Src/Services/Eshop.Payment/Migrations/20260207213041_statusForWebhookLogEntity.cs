using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Payment.Migrations
{
    /// <inheritdoc />
    public partial class statusForWebhookLogEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "WebhookLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "WebhookLog");
        }
    }
}
