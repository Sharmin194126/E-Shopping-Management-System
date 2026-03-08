using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_ShoppingManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedEmployeeId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_AssignedEmployeeId",
                table: "Products",
                column: "AssignedEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Employees_AssignedEmployeeId",
                table: "Products",
                column: "AssignedEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Employees_AssignedEmployeeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_AssignedEmployeeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AssignedEmployeeId",
                table: "Products");
        }
    }
}
