using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class LandingGearChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LandingGearUnit",
                table: "LandingGearUnit");

            migrationBuilder.DropIndex(
                name: "IX_LandingGearUnit_AircraftId",
                table: "LandingGearUnit");

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "LandingGearUnit",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LandingGearUnit",
                table: "LandingGearUnit",
                columns: new[] { "AircraftId", "Number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LandingGearUnit",
                table: "LandingGearUnit");

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "LandingGearUnit",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LandingGearUnit",
                table: "LandingGearUnit",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_LandingGearUnit_AircraftId",
                table: "LandingGearUnit",
                column: "AircraftId");
        }
    }
}
