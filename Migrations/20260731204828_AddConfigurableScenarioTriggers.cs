using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableScenarioTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualActivation",
                table: "EmergencyScenarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "TriggerAirspeedKnots",
                table: "EmergencyScenarios",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TriggerAltitudeFeet",
                table: "EmergencyScenarios",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerDelaySeconds",
                table: "EmergencyScenarios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerFlightPhase",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresManualActivation",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TriggerAirspeedKnots",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TriggerAltitudeFeet",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TriggerDelaySeconds",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TriggerFlightPhase",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "EmergencyScenarios");
        }
    }
}
