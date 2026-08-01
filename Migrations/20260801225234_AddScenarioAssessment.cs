using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxResponseSeconds",
                table: "ScenarioProcedureSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PerformanceCategory",
                table: "ScenarioProcedureSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ScoreWeight",
                table: "ScenarioProcedureSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedStepOrder",
                table: "PilotActions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeSeconds",
                table: "PilotActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WasWithinTimeLimit",
                table: "PilotActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FailureCondition",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScoringRules",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuccessCondition",
                table: "EmergencyScenarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitSeconds",
                table: "EmergencyScenarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxResponseSeconds",
                table: "ScenarioProcedureSteps");

            migrationBuilder.DropColumn(
                name: "PerformanceCategory",
                table: "ScenarioProcedureSteps");

            migrationBuilder.DropColumn(
                name: "ScoreWeight",
                table: "ScenarioProcedureSteps");

            migrationBuilder.DropColumn(
                name: "ExpectedStepOrder",
                table: "PilotActions");

            migrationBuilder.DropColumn(
                name: "ResponseTimeSeconds",
                table: "PilotActions");

            migrationBuilder.DropColumn(
                name: "WasWithinTimeLimit",
                table: "PilotActions");

            migrationBuilder.DropColumn(
                name: "FailureCondition",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "ScoringRules",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "SuccessCondition",
                table: "EmergencyScenarios");

            migrationBuilder.DropColumn(
                name: "TimeLimitSeconds",
                table: "EmergencyScenarios");
        }
    }
}
