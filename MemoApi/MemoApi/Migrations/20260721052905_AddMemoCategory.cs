using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Memos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Memos");
        }
    }
}
