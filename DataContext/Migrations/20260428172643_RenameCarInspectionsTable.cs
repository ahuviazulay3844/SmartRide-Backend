using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class RenameCarInspectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carinspection_Cars_CarId",
                table: "Carinspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Carinspection_Orders_OrderId",
                table: "Carinspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Carinspection_Users_UserId",
                table: "Carinspection");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Carinspection",
                table: "Carinspection");

            migrationBuilder.RenameTable(
                name: "Carinspection",
                newName: "CarInspections");

            migrationBuilder.RenameIndex(
                name: "IX_Carinspection_UserId",
                table: "CarInspections",
                newName: "IX_CarInspections_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Carinspection_OrderId",
                table: "CarInspections",
                newName: "IX_CarInspections_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Carinspection_CarId",
                table: "CarInspections",
                newName: "IX_CarInspections_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarInspections",
                table: "CarInspections",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarInspections_Cars_CarId",
                table: "CarInspections",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CarInspections_Orders_OrderId",
                table: "CarInspections",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CarInspections_Users_UserId",
                table: "CarInspections",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarInspections_Cars_CarId",
                table: "CarInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_CarInspections_Orders_OrderId",
                table: "CarInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_CarInspections_Users_UserId",
                table: "CarInspections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarInspections",
                table: "CarInspections");

            migrationBuilder.RenameTable(
                name: "CarInspections",
                newName: "Carinspection");

            migrationBuilder.RenameIndex(
                name: "IX_CarInspections_UserId",
                table: "Carinspection",
                newName: "IX_Carinspection_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CarInspections_OrderId",
                table: "Carinspection",
                newName: "IX_Carinspection_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_CarInspections_CarId",
                table: "Carinspection",
                newName: "IX_Carinspection_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Carinspection",
                table: "Carinspection",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Carinspection_Cars_CarId",
                table: "Carinspection",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Carinspection_Orders_OrderId",
                table: "Carinspection",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Carinspection_Users_UserId",
                table: "Carinspection",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
