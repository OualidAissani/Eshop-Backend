using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Eshop.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryType",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingPrice",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WilayaCode",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeliveryPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WilayaCode = table.Column<int>(type: "integer", nullable: false),
                    DeliveryType = table.Column<int>(type: "integer", nullable: false),
                    PriceDzd = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPrices_WilayaCode_DeliveryType",
                table: "DeliveryPrices",
                columns: new[] { "WilayaCode", "DeliveryType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryPrices");

            migrationBuilder.DropColumn(
                name: "DeliveryType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPrice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WilayaCode",
                table: "Orders");
        }
    }
}
