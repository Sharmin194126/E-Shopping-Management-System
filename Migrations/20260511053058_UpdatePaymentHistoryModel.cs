using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_ShoppingManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentHistoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAccount",
                table: "PaymentHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "PaymentHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAccount",
                table: "PaymentHistories");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "PaymentHistories");
        }
    }
}
