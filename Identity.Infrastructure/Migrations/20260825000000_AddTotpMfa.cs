using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Identity.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260825000000_AddTotpMfa")]
    public partial class AddTotpMfa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTotpEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "Users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsTotpEnabled", table: "Users");
            migrationBuilder.DropColumn(name: "TotpSecret", table: "Users");
        }
    }
}
