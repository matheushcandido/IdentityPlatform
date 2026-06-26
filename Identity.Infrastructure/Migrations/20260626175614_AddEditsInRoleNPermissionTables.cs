using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEditsInRoleNPermissionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Permissions");
        }
    }
}
