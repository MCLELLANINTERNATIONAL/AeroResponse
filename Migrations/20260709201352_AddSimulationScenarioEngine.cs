using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationScenarioEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PilotActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScenarioRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionName = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    WasInCorrectOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSafetyCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PilotActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioProcedureSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmergencyScenarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AircraftType = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Instruction = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectAction = table.Column<string>(type: "TEXT", nullable: false),
                    IsSafetyCritical = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioProcedureSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AircraftId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmergencyScenarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AircraftName = table.Column<string>(type: "TEXT", nullable: false),
                    ScenarioName = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScenarioRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ReactionTimeSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ChecklistAccuracyScore = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionMakingScore = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<int>(type: "INTEGER", nullable: false),
                    SafetyCriticalErrors = table.Column<int>(type: "INTEGER", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PilotActions");

            migrationBuilder.DropTable(
                name: "ScenarioProcedureSteps");

            migrationBuilder.DropTable(
                name: "ScenarioRuns");

            migrationBuilder.DropTable(
                name: "SimulationReports");
        }
    }
}
