using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsApp.Migrations
{
    /// <inheritdoc />
    public partial class InitDBmodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", nullable: false),
                    extension = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    username = table.Column<string>(type: "TEXT", nullable: false),
                    password = table.Column<string>(type: "TEXT", nullable: false),
                    salt = table.Column<string>(type: "TEXT", nullable: false),
                    role = table.Column<string>(type: "TEXT", nullable: true),
                    contact_id = table.Column<string>(type: "TEXT", nullable: true),
                    contactid = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_contacts_contactid",
                        column: x => x.contactid,
                        principalTable: "contacts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    token = table.Column<string>(type: "TEXT", nullable: false),
                    userid = table.Column<string>(type: "TEXT", nullable: true),
                    expiry = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revoked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.token);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_userid",
                table: "refresh_tokens",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_users_contactid",
                table: "users",
                column: "contactid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "contacts");
        }
    }
}
