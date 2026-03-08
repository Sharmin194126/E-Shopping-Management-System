using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_ShoppingManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReviewReplyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reply",
                table: "ReviewReplies",
                newName: "ReplyText");

            migrationBuilder.AddColumn<bool>(
                name: "IsSeller",
                table: "ReviewReplies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSeller",
                table: "ReviewReplies");

            migrationBuilder.RenameColumn(
                name: "ReplyText",
                table: "ReviewReplies",
                newName: "Reply");
        }
    }
}
