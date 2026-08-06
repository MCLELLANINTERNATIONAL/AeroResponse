using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class ScenarioUpdate10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerAction",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerAction",
                table: "EmergencyScenarios");
        }
    }
}
