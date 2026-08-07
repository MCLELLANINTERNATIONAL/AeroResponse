using AeroResponse.Models;
using AeroResponse.Simulation.Layouts.Aircraft;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        IServiceProvider services)
    {
        await using var scope =
            services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        // Apply any outstanding Entity Framework migrations.
        await context.Database.MigrateAsync();

        // Only add the emergency scenarios when the table is empty.
        // Existing scenarios created through CRUD will not be changed.
        if (!await context.EmergencyScenarios.AnyAsync())
        {
            var scenarios =
                new List<EmergencyScenario>
                {
                    CreateScenario(
                        title: "Engine Fire",
                        emergencyType: "Engine Fire",
                        difficulty: "Advanced",
                        description:
                            "An engine fire occurs during climb, requiring the pilot to identify the affected engine, isolate it and complete the fire procedure.",
                        triggerCondition:
                            "Engine fire warning activates during climb with increasing engine temperature and reduced engine performance.",
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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
                        expectedProcedure:
                            BuildProcedure(
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

            await context.EmergencyScenarios
                .AddRangeAsync(scenarios);

            await context.SaveChangesAsync();
        }

        // Ensure existing and newly seeded scenarios
        // have assessment rules.
        await EnsureScenarioAssessmentConfigurationAsync(
            context);

        // Seed built-in cockpit layouts independently
        // of emergency scenario seeding.
        await SeedCockpitLayoutsAsync(
            context);

        // Seed the canonical built-in aircraft fleet.
        await SeedAircraftAsync(
            context);

        // Seed demonstration reporting data separately.
        await SeedTestPilotReportsAsync(
            context);
    }


    /*=============================================================================
     |                           Aircraft Seeding                                |
     =============================================================================*/

    private static async Task SeedAircraftAsync(
        ApplicationDbContext context)
    {
        var aircraftDefinitions =
            new List<Aircraft>
            {
                new()
                {
                    Name = "Cessna 172",
                    Manufacturer = "Cessna",
                    AircraftType =
                        "Single-Engine Piston",

                    CockpitLayoutKey =
                        Cessna172CockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 1,
                    FuelTankCount = 2,
                    BrakeCount = 2,

                    CruiseSpeed = 122,
                    MaxAltitude = 14_000,

                    Description =
                        "Standard single-engine trainer aircraft.",

                    LandingGearConfig =
                        CreateTricycleLandingGear(
                            LandingGearKind.FixedTricycle),

                    IsActive = true
                },

                new()
                {
                    Name = "Gulfstream G700",

                    Manufacturer =
                        "Gulfstream Aerospace",

                    AircraftType =
                        "Large Business Jet",

                    CockpitLayoutKey =
                        GulfstreamG700CockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 2,
                    FuelTankCount = 4,
                    BrakeCount = 2,

                    CruiseSpeed = 516,
                    MaxAltitude = 51_000,

                    Description =
                        "Ultra-long-range twin-engine business jet.",

                    LandingGearConfig =
                        CreateTricycleLandingGear(
                            LandingGearKind.RetractableTricycle),

                    IsActive = true
                },

                new()
                {
                    Name = "ATR 72-600",
                    Manufacturer = "ATR",

                    AircraftType =
                        "Regional Turboprop",

                    CockpitLayoutKey =
                        Atr72600CockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 2,
                    FuelTankCount = 2,
                    BrakeCount = 2,

                    CruiseSpeed = 275,
                    MaxAltitude = 25_000,

                    Description =
                        "Twin-engine regional turboprop airliner.",

                    LandingGearConfig =
                        CreateTricycleLandingGear(
                            LandingGearKind.RetractableTricycle),

                    IsActive = true
                },

                new()
                {
                    Name =
                        "De Havilland Dash 8 Q400",

                    Manufacturer =
                        "De Havilland Canada",

                    AircraftType =
                        "Regional Turboprop",

                    CockpitLayoutKey =
                        Dash8Q400CockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 2,
                    FuelTankCount = 2,
                    BrakeCount = 2,

                    CruiseSpeed = 360,
                    MaxAltitude = 25_000,

                    Description =
                        "High-speed twin-engine regional turboprop airliner.",

                    LandingGearConfig =
                        CreateTricycleLandingGear(
                            LandingGearKind.RetractableTricycle),

                    IsActive = true
                },

                new()
                {
                    Name =
                        "Boeing 747-8 Intercontinental",

                    Manufacturer = "Boeing",

                    AircraftType =
                        "Wide-Body Commercial Jet",

                    CockpitLayoutKey =
                        Boeing747IntercontinentalCockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 4,
                    FuelTankCount = 8,
                    BrakeCount = 16,

                    CruiseSpeed = 493,
                    MaxAltitude = 43_100,

                    Description =
                        "Four-engine wide-body long-range commercial airliner.",

                    LandingGearConfig =
                        CreateBoeing747LandingGear(),

                    IsActive = true
                },

                new()
                {
                    Name = "Airbus A320-200",

                    Manufacturer = "Airbus",

                    AircraftType =
                        "Narrow-Body Commercial Jet",

                    CockpitLayoutKey =
                        AirbusA320CockpitLayout
                            .Create()
                            .Key,

                    EngineCount = 2,
                    FuelTankCount = 3,
                    BrakeCount = 4,

                    CruiseSpeed = 450,
                    MaxAltitude = 39_100,

                    Description =
                        "Twin-engine narrow-body commercial airliner.",

                    LandingGearConfig =
                        CreateTricycleLandingGear(
                            LandingGearKind.RetractableTricycle),

                    IsActive = true
                }
            };

        foreach (var definition
                 in aircraftDefinitions)
        {
            var existing =
                await context.Aircraft
                    .Include(
                        aircraft =>
                            aircraft.LandingGearConfig)
                    .ThenInclude(
                        config =>
                            config.Units)
                    .FirstOrDefaultAsync(
                        aircraft =>
                            aircraft.Name ==
                            definition.Name);

            if (existing is null)
            {
                definition.CreatedAt =
                    DateTime.UtcNow;

                context.Aircraft.Add(
                    definition);

                continue;
            }

            /*
             * Built-in aircraft are canonical.
             * Keep an existing local database record
             * synchronized with the source-controlled
             * definition.
             */

            existing.Manufacturer =
                definition.Manufacturer;

            existing.AircraftType =
                definition.AircraftType;

            existing.CockpitLayoutKey =
                definition.CockpitLayoutKey;

            existing.EngineCount =
                definition.EngineCount;

            existing.FuelTankCount =
                definition.FuelTankCount;

            existing.BrakeCount =
                definition.BrakeCount;

            existing.CruiseSpeed =
                definition.CruiseSpeed;

            existing.MaxAltitude =
                definition.MaxAltitude;

            existing.Description =
                definition.Description;

            existing.IsActive =
                definition.IsActive;

            existing.LandingGearConfig.Kind =
                definition.LandingGearConfig.Kind;

            existing.LandingGearConfig.Units.Clear();

            foreach (var unit in
                     definition
                         .LandingGearConfig
                         .Units)
            {
                existing
                    .LandingGearConfig
                    .Units
                    .Add(
                        new LandingGearUnit
                        {
                            Number =
                                unit.Number,

                            Label =
                                unit.Label,

                            Position =
                                unit.Position,

                            Status =
                                unit.Status,

                            Order =
                                unit.Order
                        });
            }
        }

        await context.SaveChangesAsync();
    }


    private static AircraftLandingGearConfig
        CreateTricycleLandingGear(
            LandingGearKind kind)
    {
        return new AircraftLandingGearConfig
        {
            Kind = kind,

            Units =
            [
                new LandingGearUnit
                {
                    Number = 1,
                    Label = "N",

                    Position =
                        LandingGearPosition.Nose,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 0
                },

                new LandingGearUnit
                {
                    Number = 2,
                    Label = "L",

                    Position =
                        LandingGearPosition.LeftMain,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 1
                },

                new LandingGearUnit
                {
                    Number = 3,
                    Label = "R",

                    Position =
                        LandingGearPosition.RightMain,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 2
                }
            ]
        };
    }


    private static AircraftLandingGearConfig
        CreateBoeing747LandingGear()
    {
        return new AircraftLandingGearConfig
        {
            Kind =
                LandingGearKind.MultiBogey,

            Units =
            [
                new LandingGearUnit
                {
                    Number = 1,
                    Label = "N",

                    Position =
                        LandingGearPosition.Nose,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 0
                },

                new LandingGearUnit
                {
                    Number = 2,
                    Label = "LW",

                    Position =
                        LandingGearPosition.LeftMain,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 1
                },

                new LandingGearUnit
                {
                    Number = 3,
                    Label = "LB",

                    Position =
                        LandingGearPosition.Custom,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 2
                },

                new LandingGearUnit
                {
                    Number = 4,
                    Label = "RB",

                    Position =
                        LandingGearPosition.Custom,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 3
                },

                new LandingGearUnit
                {
                    Number = 5,
                    Label = "RW",

                    Position =
                        LandingGearPosition.RightMain,

                    Status =
                        LandingGearStatusValue
                            .DownAndLocked,

                    Order = 4
                }
            ]
        };
    }


    /*=============================================================================
     |                         Scenario Assessment                              |
     =============================================================================*/

    private static async Task
        EnsureScenarioAssessmentConfigurationAsync(
            ApplicationDbContext context)
    {
        var scenarios =
            await context
                .EmergencyScenarios
                .ToListAsync();

        foreach (var scenario in scenarios)
        {
            scenario.TimeLimitSeconds =
                scenario.EmergencyType switch
                {
                    "Cabin Depressurization" => 90,
                    "Wind Shear" => 75,
                    "Smoke or Fire" => 90,
                    "Engine Fire" => 120,
                    _ => 150
                };

            scenario.SuccessCondition =
                "Complete all safety-critical actions, follow the expected sequence, " +
                "and achieve an overall score of at least 70% before the time limit expires.";

            scenario.FailureCondition =
                "The time limit expires, a safety-critical action is missed, " +
                "or the overall score is below 70%.";

            scenario.ScoringRules =
                "Procedure accuracy 40%; decision making 25%; time management 15%; " +
                "communication 10%; checklist usage 10%.";
        }

        await context.SaveChangesAsync();
    }


    /*=============================================================================
     |                         Cockpit Layout Seeding                           |
     =============================================================================*/

    private static async Task
        SeedCockpitLayoutsAsync(
            ApplicationDbContext context)
    {
        var definitions =
            new[]
            {
                Cessna172CockpitLayout.Create(),

                GulfstreamG700CockpitLayout.Create(),

                Atr72600CockpitLayout.Create(),

                Dash8Q400CockpitLayout.Create(),

                Boeing747IntercontinentalCockpitLayout
                    .Create(),

                AirbusA320CockpitLayout.Create()
            };

        foreach (var definition in definitions)
        {
            var existing =
                await context.CockpitLayouts
                    .FirstOrDefaultAsync(
                        layout =>
                            layout.Key ==
                            definition.Key);

            if (existing is not null)
            {
                /*
                 * Built-in layout definitions are canonical.
                 * Reseed the source-controlled definition
                 * every time the application starts.
                 */

                existing.Name =
                    definition.Name;

                existing.IsBuiltIn =
                    true;

                existing.UpdatedAt =
                    DateTime.UtcNow;

                existing.Details =
                    new CockpitLayoutDetails
                    {
                        AircraftId =
                            definition.AircraftId,

                        Rows =
                            definition.Rows,

                        Columns =
                            definition.Columns,

                        Instruments =
                            definition.Instruments,

                        EngineCount =
                            definition.EngineCount,

                        Airspeed =
                            definition.Airspeed,

                        ArtificialHorizon =
                            definition.ArtificialHorizon,

                        VSI =
                            definition.VSI,

                        DefaultState =
                            definition.DefaultState
                    };

                continue;
            }

            var now =
                DateTime.UtcNow;

            context.CockpitLayouts.Add(
                new CockpitLayout
                {
                    Key =
                        definition.Key,

                    Name =
                        definition.Name,

                    IsBuiltIn =
                        true,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now,

                    Details =
                        new CockpitLayoutDetails
                        {
                            AircraftId =
                                definition.AircraftId,

                            Rows =
                                definition.Rows,

                            Columns =
                                definition.Columns,

                            Instruments =
                                definition.Instruments,

                            EngineCount =
                                definition.EngineCount,

                            Airspeed =
                                definition.Airspeed,

                            ArtificialHorizon =
                                definition.ArtificialHorizon,

                            VSI =
                                definition.VSI,

                            DefaultState =
                                definition.DefaultState
                        }
                });
        }

        await context.SaveChangesAsync();
    }


    /*=============================================================================
     |                         Demonstration Reports                            |
     =============================================================================*/

    private static async Task
        SeedTestPilotReportsAsync(
            ApplicationDbContext context)
    {
        const string userId =
            "test-pilot";

        // Add pilot history only when this pilot
        // has no reports.
        if (!await context
                .SimulationReports
                .AnyAsync(
                    report =>
                        report.UserId ==
                        userId))
        {
            var scores =
                new[]
                {
                    61,
                    68,
                    72,
                    66,
                    75,
                    81,
                    76,
                    84,
                    88,
                    87
                };

            var scenarioNames =
                new[]
                {
                    "Engine Failure",
                    "Cabin Depressurization",
                    "Hydraulic Failure",
                    "Electrical Failure"
                };

            var startDate =
                DateTime.UtcNow.Date
                    .AddDays(-70)
                    .AddHours(14);

            var reports =
                scores
                    .Select(
                        (score, index) =>
                        {
                            var startedAt =
                                startDate
                                    .AddDays(
                                        index * 7)
                                    .AddMinutes(
                                        index * 3);

                            var completedAt =
                                startedAt
                                    .AddMinutes(6)
                                    .AddSeconds(24);

                            return new SimulationReport
                            {
                                UserId =
                                    userId,

                                PilotName =
                                    "John Doe",

                                AircraftName =
                                    index % 2 == 0
                                        ? "Boeing 737"
                                        : "Airbus A320",

                                ScenarioName =
                                    scenarioNames[
                                        index %
                                        scenarioNames.Length],

                                Difficulty =
                                    index > 6
                                        ? "Advanced"
                                        : "Intermediate",

                                StartedAt =
                                    startedAt,

                                CompletedAt =
                                    completedAt,

                                ActionsTaken =
                                    36 + index,

                                ReactionTimeSeconds =
                                    Math.Max(
                                        7,
                                        18 - index),

                                ChecklistAccuracyScore =
                                    Math.Min(
                                        96,
                                        score + 3),

                                DecisionMakingScore =
                                    Math.Min(
                                        94,
                                        score - 2),

                                TimeManagementScore =
                                    Math.Min(
                                        92,
                                        score - 7),

                                CommunicationScore =
                                    Math.Min(
                                        93,
                                        score - 5),

                                ChecklistUsageScore =
                                    Math.Min(
                                        100,
                                        score + 1),

                                OverallScore =
                                    score,

                                SafetyCriticalErrors =
                                    score >= 75
                                        ? 0
                                        : 1,

                                Outcome =
                                    score >= 70
                                        ? "Scenario passed. Aircraft stabilised and passengers safeguarded."
                                        : "Further practice required.",

                                Feedback =
                                    score >= 80
                                        ? "Great job. You demonstrated strong situational awareness and followed protocols effectively."
                                        : "Review the emergency sequence and practise the first response actions.",

                                AiFeedback =
                                    score >= 80
                                        ? "Strong procedural discipline and calm decision-making. Improve the time taken to identify the primary failure and communicate intentions earlier."
                                        : "Use the checklist in strict sequence and confirm the affected system before taking safety-critical action.",

                                CreatedAt =
                                    completedAt
                            };
                        })
                    .ToList();

            context.SimulationReports
                .AddRange(reports);

            context.PilotAchievements
                .AddRange(
                    new PilotAchievement
                    {
                        UserId =
                            userId,

                        Code =
                            "quick-thinker",

                        Name =
                            "Quick Thinker",

                        Description =
                            "Responded rapidly to an emergency.",

                        Icon =
                            "⚡",

                        EarnedAt =
                            reports[^1]
                                .CreatedAt
                    },

                    new PilotAchievement
                    {
                        UserId =
                            userId,

                        Code =
                            "checklist-master",

                        Name =
                            "Checklist Master",

                        Description =
                            "Demonstrated excellent checklist accuracy.",

                        Icon =
                            "✓",

                        EarnedAt =
                            reports[^1]
                                .CreatedAt
                    },

                    new PilotAchievement
                    {
                        UserId =
                            userId,

                        Code =
                            "calm-pressure",

                        Name =
                            "Calm Under Pressure",

                        Description =
                            "Maintained control under pressure.",

                        Icon =
                            "✈",

                        EarnedAt =
                            reports[^1]
                                .CreatedAt
                    },

                    new PilotAchievement
                    {
                        UserId =
                            userId,

                        Code =
                            "protocol-pro",

                        Name =
                            "Protocol Pro",

                        Description =
                            "Completed a scenario without a critical error.",

                        Icon =
                            "⚓",

                        EarnedAt =
                            reports[^2]
                                .CreatedAt
                    },

                    new PilotAchievement
                    {
                        UserId =
                            userId,

                        Code =
                            "communication-star",

                        Name =
                            "Communication Star",

                        Description =
                            "Used clear emergency communication.",

                        Icon =
                            "★",

                        EarnedAt =
                            reports[^3]
                                .CreatedAt
                    }
                );

            await context.SaveChangesAsync();
        }

        // Ensure the demonstration pilot
        // has a Bird Strike result.
        var birdStrikeExists =
            await context
                .SimulationReports
                .AnyAsync(
                    report =>
                        report.UserId ==
                        userId &&
                        report.ScenarioName
                            .StartsWith(
                                "Bird Strike"));

        if (!birdStrikeExists)
        {
            var startedAt =
                DateTime.UtcNow
                    .AddMinutes(-6)
                    .AddSeconds(-24);

            var completedAt =
                startedAt
                    .AddMinutes(6)
                    .AddSeconds(24);

            var birdStrikeReport =
                new SimulationReport
                {
                    UserId =
                        userId,

                    PilotName =
                        "John Doe",

                    AircraftName =
                        "Boeing 737",

                    ScenarioName =
                        "Bird Strike – Takeoff",

                    Difficulty =
                        "Advanced",

                    StartedAt =
                        startedAt,

                    CompletedAt =
                        completedAt,

                    ActionsTaken =
                        46,

                    ReactionTimeSeconds =
                        8,

                    ChecklistAccuracyScore =
                        90,

                    DecisionMakingScore =
                        85,

                    TimeManagementScore =
                        80,

                    CommunicationScore =
                        82,

                    ChecklistUsageScore =
                        88,

                    OverallScore =
                        87,

                    SafetyCriticalErrors =
                        0,

                    Outcome =
                        "Scenario passed. The affected engine was assessed, the aircraft was stabilised and a safe return was initiated.",

                    Feedback =
                        "Great job. You maintained aircraft control, identified the bird-strike indications and followed the emergency procedure effectively.",

                    AiFeedback =
                        "Strong situational awareness and procedural discipline. Continue improving the speed of the initial engine assessment and communicate the return plan earlier.",

                    CreatedAt =
                        completedAt
                };

            context.SimulationReports.Add(
                birdStrikeReport);

            await context.SaveChangesAsync();
        }
    }


    /*=============================================================================
     |                            Scenario Helpers                              |
     =============================================================================*/

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
            Title =
                title,

            EmergencyType =
                emergencyType,

            Difficulty =
                difficulty,

            Description =
                description,

            TriggerCondition =
                triggerCondition,

            ExpectedProcedure =
                expectedProcedure.Trim(),

            IsActive =
                true,

            CreatedAt =
                DateTime.UtcNow
        };
    }


    private static string BuildProcedure(
        params string[] steps)
    {
        return string.Join(
            Environment.NewLine,
            steps);
    }
}