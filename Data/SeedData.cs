using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        // Apply any outstanding Entity Framework migrations.
        await context.Database.MigrateAsync();

        // Only add the emergency scenarios when the table is empty.
        // Existing scenarios created through CRUD will not be changed.
        if (!await context.EmergencyScenarios.AnyAsync())
        {
            var scenarios = new List<EmergencyScenario>
            {
                CreateScenario(
                    title: "Engine Fire",
                    emergencyType: "Engine Fire",
                    difficulty: "Advanced",
                    description:
                        "An engine fire occurs during climb, requiring the pilot to identify the affected engine, isolate it and complete the fire procedure.",
                    triggerCondition:
                        "Engine fire warning activates during climb with increasing engine temperature and reduced engine performance.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Reduce thrust on the affected engine.",
                        "Confirm the affected engine.",
                        "Cut off fuel to the affected engine.",
                        "Shut down the affected engine.",
                        "Pull the fire handle.",
                        "Discharge the fire bottle.",
                        "Declare an emergency.",
                        "Divert to the nearest suitable airport.",
                        "Prepare the passengers and aircraft for landing."
                    )
                ),

                CreateScenario(
                    title: "Engine Failure",
                    emergencyType: "Engine Failure",
                    difficulty: "Advanced",
                    description:
                        "One engine loses thrust, requiring the pilot to stabilise the aircraft and prepare for single-engine operation and landing.",
                    triggerCondition:
                        "Engine thrust drops to zero during flight and the aircraft begins to yaw towards the failed engine.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Stabilise airspeed, altitude and heading.",
                        "Identify the failed engine.",
                        "Reduce thrust on the affected engine.",
                        "Shut down and secure the failed engine.",
                        "Declare an emergency.",
                        "Plan a diversion.",
                        "Prepare for a single-engine landing."
                    )
                ),

                CreateScenario(
                    title: "Bird Strike",
                    emergencyType: "Bird Strike",
                    difficulty: "Intermediate",
                    description:
                        "A bird strike causes possible engine or airframe damage during departure or approach.",
                    triggerCondition:
                        "A bird impact occurs at low altitude and one engine begins showing reduced performance.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Assess engine and aircraft performance.",
                        "Check for abnormal vibration, temperature or pressure.",
                        "Reduce affected-engine thrust if necessary.",
                        "Declare an emergency.",
                        "Return to or divert to a suitable airport.",
                        "Prepare for landing and inspection."
                    )
                ),

                CreateScenario(
                    title: "Cabin Depressurization",
                    emergencyType: "Cabin Depressurization",
                    difficulty: "Advanced",
                    description:
                        "Cabin pressure is lost at cruising altitude, requiring immediate oxygen use and an emergency descent.",
                    triggerCondition:
                        "The cabin altitude warning activates during cruise and cabin pressure continues to decrease.",
                    expectedProcedure: BuildProcedure(
                        "Don oxygen masks.",
                        "Establish crew communication.",
                        "Begin an emergency descent.",
                        "Set the emergency transponder code.",
                        "Declare an emergency.",
                        "Descend to a safe altitude.",
                        "Assess passenger and cabin conditions.",
                        "Divert to the nearest suitable airport."
                    )
                ),

                CreateScenario(
                    title: "Hydraulic Failure",
                    emergencyType: "Hydraulic Failure",
                    difficulty: "Advanced",
                    description:
                        "A hydraulic system fails, affecting aircraft controls, braking, flaps or landing-gear operation.",
                    triggerCondition:
                        "Hydraulic pressure falls below its safe operating range and a system warning appears.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Identify the failed hydraulic system.",
                        "Check affected flight controls and aircraft systems.",
                        "Activate the available backup or alternate system.",
                        "Declare an emergency if required.",
                        "Review landing limitations.",
                        "Prepare for an abnormal landing."
                    )
                ),

                CreateScenario(
                    title: "Electrical Failure",
                    emergencyType: "Electrical Failure",
                    difficulty: "Intermediate",
                    description:
                        "The primary electrical supply fails, reducing the availability of instruments, navigation and communication systems.",
                    triggerCondition:
                        "Primary electrical generation is lost and non-essential cockpit systems begin shutting down.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Confirm the electrical failure.",
                        "Activate backup electrical power.",
                        "Shed non-essential electrical loads.",
                        "Check essential flight instruments.",
                        "Declare an emergency if required.",
                        "Plan a diversion using available systems.",
                        "Prepare for landing."
                    )
                ),

                CreateScenario(
                    title: "Fuel Leak",
                    emergencyType: "Fuel Leak",
                    difficulty: "Advanced",
                    description:
                        "An abnormal fuel loss or imbalance develops, requiring the pilot to identify and isolate the affected fuel system.",
                    triggerCondition:
                        "Fuel quantity decreases unexpectedly and a significant imbalance develops between fuel tanks.",
                    expectedProcedure: BuildProcedure(
                        "Maintain aircraft control.",
                        "Monitor fuel quantity and balance.",
                        "Identify the likely source of the leak.",
                        "Isolate the affected fuel source where appropriate.",
                        "Avoid transferring fuel in a way that worsens the leak.",
                        "Declare an emergency.",
                        "Divert to the nearest suitable airport.",
                        "Prepare for landing."
                    )
                ),

                CreateScenario(
                    title: "Landing Gear Malfunction",
                    emergencyType: "Landing Gear Malfunction",
                    difficulty: "Intermediate",
                    description:
                        "The landing gear fails to extend normally or does not indicate that it is safely locked.",
                    triggerCondition:
                        "The landing-gear lever is selected down but one or more gear indicators remain unsafe.",
                    expectedProcedure: BuildProcedure(
                        "Go around if the approach is unstable.",
                        "Maintain a safe altitude and airspeed.",
                        "Check landing-gear indications.",
                        "Attempt the alternate gear-extension procedure.",
                        "Confirm the available gear position.",
                        "Declare an emergency.",
                        "Prepare the cabin and passengers.",
                        "Complete an emergency landing."
                    )
                ),

                CreateScenario(
                    title: "Smoke or Fire",
                    emergencyType: "Smoke or Fire",
                    difficulty: "Expert",
                    description:
                        "Smoke or fire develops in the cockpit or cabin, requiring immediate identification, suppression and landing.",
                    triggerCondition:
                        "Smoke is detected in the cockpit or cabin and the source is initially unknown.",
                    expectedProcedure: BuildProcedure(
                        "Don oxygen masks.",
                        "Establish crew communication.",
                        "Identify the smoke or fire source.",
                        "Isolate the affected electrical or aircraft system.",
                        "Activate the appropriate fire-suppression equipment.",
                        "Declare an emergency.",
                        "Begin an immediate diversion.",
                        "Prepare for evacuation if required.",
                        "Land as soon as possible."
                    )
                ),

                CreateScenario(
                    title: "Wind Shear",
                    emergencyType: "Wind Shear",
                    difficulty: "Expert",
                    description:
                        "A severe wind-shear encounter occurs close to the ground during take-off or landing.",
                    triggerCondition:
                        "A wind-shear warning activates and the aircraft experiences rapid changes in airspeed and vertical speed.",
                    expectedProcedure: BuildProcedure(
                        "Apply maximum available thrust.",
                        "Maintain the required pitch attitude.",
                        "Follow flight-director wind-shear guidance where available.",
                        "Do not change aircraft configuration until clear.",
                        "Monitor altitude, airspeed and vertical speed.",
                        "Continue the escape manoeuvre until safely clear.",
                        "Advise air traffic control when workload permits.",
                        "Reassess the approach or diversion plan."
                    )
                )
            };

            await context.EmergencyScenarios.AddRangeAsync(scenarios);
            await context.SaveChangesAsync();
        }

        // Seed the demonstration reporting data separately.
        // This still runs when emergency scenarios already exist.
        await SeedTestPilotReportsAsync(context);
    }

    private static async Task SeedTestPilotReportsAsync(
        ApplicationDbContext context)
    {
        const string userId = "test-pilot";

        // Add pilot history only when this pilot has no reports.
        if (!await context.SimulationReports
                .AnyAsync(report => report.UserId == userId))
        {
            var scores = new[]
            {
                61, 68, 72, 66, 75, 81, 76, 84, 88, 87
            };

            var scenarioNames = new[]
            {
                "Engine Failure",
                "Cabin Depressurization",
                "Hydraulic Failure",
                "Electrical Failure"
            };

            var startDate = DateTime.UtcNow.Date
                .AddDays(-70)
                .AddHours(14);

            var reports = scores
                .Select((score, index) =>
                {
                    var startedAt = startDate
                        .AddDays(index * 7)
                        .AddMinutes(index * 3);

                    var completedAt = startedAt
                        .AddMinutes(6)
                        .AddSeconds(24);

                    return new SimulationReport
                    {
                        UserId = userId,
                        PilotName = "John Doe",

                        AircraftName = index % 2 == 0
                            ? "Boeing 737"
                            : "Airbus A320",

                        ScenarioName =
                            scenarioNames[index % scenarioNames.Length],

                        Difficulty = index > 6
                            ? "Advanced"
                            : "Intermediate",

                        StartedAt = startedAt,
                        CompletedAt = completedAt,
                        ActionsTaken = 36 + index,

                        ReactionTimeSeconds =
                            Math.Max(7, 18 - index),

                        ChecklistAccuracyScore =
                            Math.Min(96, score + 3),

                        DecisionMakingScore =
                            Math.Min(94, score - 2),

                        TimeManagementScore =
                            Math.Min(92, score - 7),

                        CommunicationScore =
                            Math.Min(93, score - 5),

                        ChecklistUsageScore =
                            Math.Min(100, score + 1),

                        OverallScore = score,

                        SafetyCriticalErrors =
                            score >= 75 ? 0 : 1,

                        Outcome = score >= 70
                            ? "Scenario passed. Aircraft stabilised and passengers safeguarded."
                            : "Further practice required.",

                        Feedback = score >= 80
                            ? "Great job. You demonstrated strong situational awareness and followed protocols effectively."
                            : "Review the emergency sequence and practise the first response actions.",

                        AiFeedback = score >= 80
                            ? "Strong procedural discipline and calm decision-making. Improve the time taken to identify the primary failure and communicate intentions earlier."
                            : "Use the checklist in strict sequence and confirm the affected system before taking safety-critical action.",

                        CreatedAt = completedAt
                    };
                })
                .ToList();

            context.SimulationReports.AddRange(reports);

            context.PilotAchievements.AddRange(
                new PilotAchievement
                {
                    UserId = userId,
                    Code = "quick-thinker",
                    Name = "Quick Thinker",
                    Description =
                        "Responded rapidly to an emergency.",
                    Icon = "⚡",
                    EarnedAt = reports[^1].CreatedAt
                },
                new PilotAchievement
                {
                    UserId = userId,
                    Code = "checklist-master",
                    Name = "Checklist Master",
                    Description =
                        "Demonstrated excellent checklist accuracy.",
                    Icon = "✓",
                    EarnedAt = reports[^1].CreatedAt
                },
                new PilotAchievement
                {
                    UserId = userId,
                    Code = "calm-pressure",
                    Name = "Calm Under Pressure",
                    Description =
                        "Maintained control under pressure.",
                    Icon = "✈",
                    EarnedAt = reports[^1].CreatedAt
                },
                new PilotAchievement
                {
                    UserId = userId,
                    Code = "protocol-pro",
                    Name = "Protocol Pro",
                    Description =
                        "Completed a scenario without a critical error.",
                    Icon = "⚓",
                    EarnedAt = reports[^2].CreatedAt
                },
                new PilotAchievement
                {
                    UserId = userId,
                    Code = "communication-star",
                    Name = "Communication Star",
                    Description =
                        "Used clear emergency communication.",
                    Icon = "★",
                    EarnedAt = reports[^3].CreatedAt
                }
            );

            await context.SaveChangesAsync();
        }

        // Ensure the demonstration pilot has a Bird Strike result.
        // It will be the latest dashboard result.
        var birdStrikeExists =
            await context.SimulationReports.AnyAsync(report =>
                report.UserId == userId &&
                report.ScenarioName.StartsWith("Bird Strike"));

        if (!birdStrikeExists)
        {
            var startedAt = DateTime.UtcNow
                .AddMinutes(-6)
                .AddSeconds(-24);

            var completedAt = startedAt
                .AddMinutes(6)
                .AddSeconds(24);

            var birdStrikeReport = new SimulationReport
            {
                UserId = userId,
                PilotName = "John Doe",
                AircraftName = "Boeing 737",
                ScenarioName = "Bird Strike – Takeoff",
                Difficulty = "Advanced",
                StartedAt = startedAt,
                CompletedAt = completedAt,
                ActionsTaken = 46,
                ReactionTimeSeconds = 8,
                ChecklistAccuracyScore = 90,
                DecisionMakingScore = 85,
                TimeManagementScore = 80,
                CommunicationScore = 82,
                ChecklistUsageScore = 88,
                OverallScore = 87,
                SafetyCriticalErrors = 0,

                Outcome =
                    "Scenario passed. The affected engine was assessed, the aircraft was stabilised and a safe return was initiated.",

                Feedback =
                    "Great job. You maintained aircraft control, identified the bird-strike indications and followed the emergency procedure effectively.",

                AiFeedback =
                    "Strong situational awareness and procedural discipline. Continue improving the speed of the initial engine assessment and communicate the return plan earlier.",

                CreatedAt = completedAt
            };

            context.SimulationReports.Add(birdStrikeReport);
            await context.SaveChangesAsync();
        }
    }

    private static EmergencyScenario CreateScenario(
        string title,
        string emergencyType,
        string difficulty,
        string description,
        string triggerCondition,
        string expectedProcedure)
    {
        return new EmergencyScenario
        {
            Title = title,
            EmergencyType = emergencyType,
            Difficulty = difficulty,
            Description = description,
            TriggerCondition = triggerCondition,
            ExpectedProcedure = expectedProcedure.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildProcedure(params string[] steps)
    {
        return string.Join(Environment.NewLine, steps);
    }
}