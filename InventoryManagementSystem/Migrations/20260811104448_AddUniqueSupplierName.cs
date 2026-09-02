using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSupplierName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-route any products linked to duplicate suppliers to the master supplier record (lowest ID)
            migrationBuilder.Sql(@"
                UPDATE Products
                SET SupplierId = MasterSuppliers.MasterId
                FROM Products p
                INNER JOIN Suppliers s ON p.SupplierId = s.Id
                INNER JOIN (
                    SELECT Name, MIN(Id) AS MasterId
                    FROM Suppliers
                    GROUP BY Name
                ) MasterSuppliers ON s.Name = MasterSuppliers.Name
                WHERE s.Id <> MasterSuppliers.MasterId;
            ");

            // Delete duplicate supplier records (keeping the master record with the lowest ID)
            migrationBuilder.Sql(@"
                DELETE s1 
                FROM Suppliers s1 
                INNER JOIN Suppliers s2 ON s1.Name = s2.Name AND s1.Id > s2.Id;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers");
        }
    }
}
