using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_ShoppingManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLastNotificationCheckToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotificationCheck",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNotificationCheck",
                table: "AspNetUsers");
        }
    }
}
