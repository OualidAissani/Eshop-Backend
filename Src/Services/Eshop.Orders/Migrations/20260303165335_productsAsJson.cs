using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Orders.Migrations
{
    /// <inheritdoc />
    public partial class productsAsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductsJson",
                table: "OrderStates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductsJson",
                table: "OrderStates");
        }
    }
}
