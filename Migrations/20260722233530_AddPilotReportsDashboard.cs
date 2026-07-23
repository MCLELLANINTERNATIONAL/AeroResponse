using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroResponse.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotReportsDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionsTaken",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AiFeedback",
                table: "SimulationReports",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AircraftName",
                table: "SimulationReports",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ChecklistUsageScore",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationScore",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "SimulationReports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "SimulationReports",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Passed",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PilotName",
                table: "SimulationReports",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProcedureAccuracyScore",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScenarioName",
                table: "SimulationReports",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "SimulationReports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TimeManagementScore",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTimeSeconds",
                table: "SimulationReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PilotAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PilotAchievements", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PilotAchievements");

            migrationBuilder.DropColumn(
                name: "ActionsTaken",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "AircraftName",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "ChecklistUsageScore",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "CommunicationScore",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "Passed",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "PilotName",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "ProcedureAccuracyScore",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "ScenarioName",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "TimeManagementScore",
                table: "SimulationReports");

            migrationBuilder.DropColumn(
                name: "TotalTimeSeconds",
                table: "SimulationReports");
        }
    }
}
