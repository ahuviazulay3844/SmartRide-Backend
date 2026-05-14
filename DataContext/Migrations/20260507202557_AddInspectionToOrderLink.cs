using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionToOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "CarInspections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarInspections_OrderId1",
                table: "CarInspections",
                column: "OrderId1",
                unique: true,
                filter: "[OrderId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CarInspections_Orders_OrderId1",
                table: "CarInspections",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarInspections_Orders_OrderId1",
                table: "CarInspections");

            migrationBuilder.DropIndex(
                name: "IX_CarInspections_OrderId1",
                table: "CarInspections");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "CarInspections");
        }
    }
}
