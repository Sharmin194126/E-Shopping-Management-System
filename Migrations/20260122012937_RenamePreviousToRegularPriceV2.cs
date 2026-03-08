using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_ShoppingManagement.Migrations
{
    /// <inheritdoc />
    public partial class RenamePreviousToRegularPriceV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreviousPrice",
                table: "Products",
                newName: "RegularPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegularPrice",
                table: "Products",
                newName: "PreviousPrice");
        }
    }
}
