using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchRatesToPurchaseItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DemandRate",
                table: "Purchases",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixRate",
                table: "Purchases",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DemandRate",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixRate",
                table: "PurchaseItems",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemandRate",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "FixRate",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "DemandRate",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "FixRate",
                table: "PurchaseItems");
        }
    }
}
