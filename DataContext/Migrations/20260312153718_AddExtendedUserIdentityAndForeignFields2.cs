using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedUserIdentityAndForeignFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseImageUri",
                table: "Users");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicenseImageUri",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
