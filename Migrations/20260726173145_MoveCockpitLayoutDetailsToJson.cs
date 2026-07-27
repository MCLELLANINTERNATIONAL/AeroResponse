using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class MoveCockpitLayoutDetailsToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Columns",
                table: "CockpitLayouts");

            migrationBuilder.DropColumn(
                name: "Rows",
                table: "CockpitLayouts");

            migrationBuilder.RenameColumn(
                name: "Instruments",
                table: "CockpitLayouts",
                newName: "Details");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Details",
                table: "CockpitLayouts",
                newName: "Instruments");

            migrationBuilder.AddColumn<int>(
                name: "Columns",
                table: "CockpitLayouts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rows",
                table: "CockpitLayouts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
