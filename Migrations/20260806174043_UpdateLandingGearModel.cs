using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLandingGearModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValidationType",
                table: "ScenarioProcedureSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationType",
                table: "ScenarioProcedureSteps");
        }
    }
}
