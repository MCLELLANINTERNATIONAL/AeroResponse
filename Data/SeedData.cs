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

        // Apply any outstanding migrations before seeding.
        await context.Database.MigrateAsync();

        // Do not add duplicates if scenarios already exist.
        if (await context.EmergencyScenarios.AnyAsync())
        {
            return;
        }

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
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Reduce thrust on the affected engine.
                    Confirm the affected engine.
                    Cut off fuel to the affected engine.
                    Shut down the affected engine.
                    Pull the fire handle.
                    Discharge the fire bottle.
                    Declare an emergency.
                    Divert to the nearest suitable airport.
                    Prepare the passengers and aircraft for landing.
                    """),

            CreateScenario(
                title: "Engine Failure",
                emergencyType: "Engine Failure",
                difficulty: "Advanced",
                description:
                    "One engine loses thrust, requiring the pilot to stabilise the aircraft and prepare for single-engine operation and landing.",
                triggerCondition:
                    "Engine thrust drops to zero during flight and the aircraft begins to yaw towards the failed engine.",
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Stabilise airspeed, altitude and heading.
                    Identify the failed engine.
                    Reduce thrust on the affected engine.
                    Shut down and secure the failed engine.
                    Declare an emergency.
                    Plan a diversion.
                    Prepare for a single-engine landing.
                    """),

            CreateScenario(
                title: "Bird Strike",
                emergencyType: "Bird Strike",
                difficulty: "Intermediate",
                description:
                    "A bird strike causes possible engine or airframe damage during departure or approach.",
                triggerCondition:
                    "A bird impact occurs at low altitude and one engine begins showing reduced performance.",
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Assess engine and aircraft performance.
                    Check for abnormal vibration, temperature or pressure.
                    Reduce affected-engine thrust if necessary.
                    Declare an emergency.
                    Return to or divert to a suitable airport.
                    Prepare for landing and inspection.
                    """),

            CreateScenario(
                title: "Cabin Depressurization",
                emergencyType: "Cabin Depressurization",
                difficulty: "Advanced",
                description:
                    "Cabin pressure is lost at cruising altitude, requiring immediate oxygen use and an emergency descent.",
                triggerCondition:
                    "The cabin altitude warning activates during cruise and cabin pressure continues to decrease.",
                expectedProcedure:
                    """
                    Don oxygen masks.
                    Establish crew communication.
                    Begin an emergency descent.
                    Set the emergency transponder code.
                    Declare an emergency.
                    Descend to a safe altitude.
                    Assess passenger and cabin conditions.
                    Divert to the nearest suitable airport.
                    """),

            CreateScenario(
                title: "Hydraulic Failure",
                emergencyType: "Hydraulic Failure",
                difficulty: "Advanced",
                description:
                    "A hydraulic system fails, affecting aircraft controls, braking, flaps or landing-gear operation.",
                triggerCondition:
                    "Hydraulic pressure falls below its safe operating range and a system warning appears.",
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Identify the failed hydraulic system.
                    Check affected flight controls and aircraft systems.
                    Activate the available backup or alternate system.
                    Declare an emergency if required.
                    Review landing limitations.
                    Prepare for an abnormal landing.
                    """),

            CreateScenario(
                title: "Electrical Failure",
                emergencyType: "Electrical Failure",
                difficulty: "Intermediate",
                description:
                    "The primary electrical supply fails, reducing the availability of instruments, navigation and communication systems.",
                triggerCondition:
                    "Primary electrical generation is lost and non-essential cockpit systems begin shutting down.",
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Confirm the electrical failure.
                    Activate backup electrical power.
                    Shed non-essential electrical loads.
                    Check essential flight instruments.
                    Declare an emergency if required.
                    Plan a diversion using available systems.
                    Prepare for landing.
                    """),

            CreateScenario(
                title: "Fuel Leak",
                emergencyType: "Fuel Leak",
                difficulty: "Advanced",
                description:
                    "An abnormal fuel loss or imbalance develops, requiring the pilot to identify and isolate the affected fuel system.",
                triggerCondition:
                    "Fuel quantity decreases unexpectedly and a significant imbalance develops between fuel tanks.",
                expectedProcedure:
                    """
                    Maintain aircraft control.
                    Monitor fuel quantity and balance.
                    Identify the likely source of the leak.
                    Isolate the affected fuel source where appropriate.
                    Avoid transferring fuel in a way that worsens the leak.
                    Declare an emergency.
                    Divert to the nearest suitable airport.
                    Prepare for landing.
                    """),

            CreateScenario(
                title: "Landing Gear Malfunction",
                emergencyType: "Landing Gear Malfunction",
                difficulty: "Intermediate",
                description:
                    "The landing gear fails to extend normally or does not indicate that it is safely locked.",
                triggerCondition:
                    "The landing-gear lever is selected down but one or more gear indicators remain unsafe.",
                expectedProcedure:
                    """
                    Go around if the approach is unstable.
                    Maintain a safe altitude and airspeed.
                    Check landing-gear indications.
                    Attempt the alternate gear-extension procedure.
                    Confirm the available gear position.
                    Declare an emergency.
                    Prepare the cabin and passengers.
                    Complete an emergency landing.
                    """),

            CreateScenario(
                title: "Smoke or Fire",
                emergencyType: "Smoke or Fire",
                difficulty: "Expert",
                description:
                    "Smoke or fire develops in the cockpit or cabin, requiring immediate identification, suppression and landing.",
                triggerCondition:
                    "Smoke is detected in the cockpit or cabin and the source is initially unknown.",
                expectedProcedure:
                    """
                    Don oxygen masks.
                    Establish crew communication.
                    Identify the smoke or fire source.
                    Isolate the affected electrical or aircraft system.
                    Activate the appropriate fire-suppression equipment.
                    Declare an emergency.
                    Begin an immediate diversion.
                    Prepare for evacuation if required.
                    Land as soon as possible.
                    """),

            CreateScenario(
                title: "Wind Shear",
                emergencyType: "Wind Shear",
                difficulty: "Expert",
                description:
                    "A severe wind-shear encounter occurs close to the ground during take-off or landing.",
                triggerCondition:
                    "A wind-shear warning activates and the aircraft experiences rapid changes in airspeed and vertical speed.",
                expectedProcedure:
                    """
                    Apply maximum available thrust.
                    Maintain the required pitch attitude.
                    Follow flight-director wind-shear guidance where available.
                    Do not change aircraft configuration until clear.
                    Monitor altitude, airspeed and vertical speed.
                    Continue the escape manoeuvre until safely clear.
                    Advise air traffic control when workload permits.
                    Reassess the approach or diversion plan.
                    """)
        };

        await context.EmergencyScenarios.AddRangeAsync(scenarios);
        await context.SaveChangesAsync();
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
}