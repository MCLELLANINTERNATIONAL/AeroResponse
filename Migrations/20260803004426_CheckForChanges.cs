using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class CheckForChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScenarioProcedureSteps_EmergencyScenarioId_AircraftType_StepOrder",
                table: "ScenarioProcedureSteps",
                columns: new[] { "EmergencyScenarioId", "AircraftType", "StepOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioProcedureSteps_EmergencyScenarios_EmergencyScenarioId",
                table: "ScenarioProcedureSteps",
                column: "EmergencyScenarioId",
                principalTable: "EmergencyScenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioProcedureSteps_EmergencyScenarios_EmergencyScenarioId",
                table: "ScenarioProcedureSteps");

            migrationBuilder.DropIndex(
                name: "IX_ScenarioProcedureSteps_EmergencyScenarioId_AircraftType_StepOrder",
                table: "ScenarioProcedureSteps");
        }
    }
}
