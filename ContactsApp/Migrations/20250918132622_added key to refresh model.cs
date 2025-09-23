using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsApp.Migrations
{
    /// <inheritdoc />
    public partial class addedkeytorefreshmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<string>(
                name: "id",
                table: "refresh_tokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "id",
                table: "refresh_tokens");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "token");
        }
    }
}
