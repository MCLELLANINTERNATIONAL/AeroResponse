using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;
using AeroResponse.Simulation.Scenarios;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using AeroResponse.Services;
using SimulationSelectionModel = AeroResponse.Models.SimulationSelection;
using VSIMath = AeroResponse.Simulation.Instruments.VerticalSpeedIndicator.VSIMath;
namespace AeroResponse.Components.Pages;

public partial class Simulation : IAsyncDisposable
{

/* ====================================================================================================
 |                                      Variable Decleration                                           |
 ====================================================================================================== */

    [Inject]
    private ILogger<Simulation> Logger { get; set; } = default!;
    private CockpitState cockpitState = new();
    private bool isOnGround = false;
    private bool _showEmergencyModal;

    private EmergencyScenario
        selectedScenarioRecord = default!;

    private ISimulationScenario
        selectedRuntimeScenario = default!;

    private List<ScenarioProcedureStep>
        procedureSteps = [];

    private readonly HashSet<int>
        _completedProcedureStepOrders = [];

    private SimulationReport? _completedReport;

    private bool _isCompleting;

    private int _remainingSeconds;

    // Voice control / AI instructor state
    private DotNetObjectReference<Simulation>? _voiceReference;
    private bool _voiceSupported;
    private bool _voiceListening;
    private string _voiceStatus = "Select Start Voice Control.";
    private string? _lastVoiceTranscript;
    private bool _emergencyModalHasBeenShown = false;
    private AiInstructorFeedback? _latestInstructorFeedback;

    private IReadOnlyList<EmergencyScenario>
        _scenarioRecords = [];

    private PeriodicTimer? _simulationTimer;

    private CancellationTokenSource?
        _simulationCancellation;

    private Task? _simulationLoop;

    private DateTime simulationStartedAt;

    private bool emergencyTriggered;

    private bool manualTriggerRequested;

    private bool _isReady;
    private bool _loadFailed;
    private bool _needsStorageCheck;
    private bool _isAircraftMenuOpen;
    private bool _isScenarioMenuOpen;
    private IReadOnlyList<Aircraft>
        _aircraftOptions = [];

    private Aircraft
        selectedAircraft = default!;

    [Parameter]
    public string? AircraftKey { get; set; }

    [Parameter]
    public string? ScenarioType { get; set; }

    private string GetFireLabel()
    {
        if (selectedScenarioRecord.EmergencyType == "Engine Fire")
        {
            var engine = GetAffectedEngine();
            return engine is null ? "ENGINE FIRE" : $"ENGINE {engine.Number} FIRE";
        }

        return "SMOKE / FIRE";
    }

    private CockpitLayoutDefinition
        cockpitLayout = default!;

    private int CompletedStepCount =>
        procedureSteps.Count(step =>
            _completedProcedureStepOrders.Contains(
                step.StepOrder));

    private double ChecklistProgressPercent =>
        procedureSteps.Count == 0
            ? 0
            : Math.Clamp(
                CompletedStepCount * 100.0 /
                procedureSteps.Count,
                0,
                100);


    /* ==  Initiliazation and state based functions == 
    protected override async Task OnInitializedAsync()
    {
        await LoadSelectionAsync(
            AircraftKey,
            ScenarioType,
            saveSelection: false);
    }*/

/* ====================================================================================================
 |                                      State Based Actions                                            |
 ====================================================================================================== */

    protected override async Task OnParametersSetAsync()
    {
        _isReady = false;
        _loadFailed = false;

        var allAircraft =
            await AircraftService.GetAllAsync();

        var availableLayoutKeys =
            (await LayoutProvider.GetLayouts())
                .Select(layout => layout.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _aircraftOptions = allAircraft
            .Where(aircraft =>
                !string.IsNullOrWhiteSpace(
                    aircraft.CockpitLayoutKey) &&
                availableLayoutKeys.Contains(
                    aircraft.CockpitLayoutKey))
            .ToArray();

        _scenarioRecords =
            await ScenarioDataService
                .GetActiveScenariosAsync();

        CloseSelectorMenus();

        if (!string.IsNullOrWhiteSpace(AircraftKey) &&
            !string.IsNullOrWhiteSpace(ScenarioType))
        {
            await LoadSelectionAsync(
                AircraftKey,
                ScenarioType);

            return;
        }

        _needsStorageCheck = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // JS interop must occur after the component has rendered.
        if (firstRender)
        {
            _voiceReference = DotNetObjectReference.Create(this);

            var status =
                await JSRuntime.InvokeAsync<VoiceInitializationResult>(
                    "aeroVoice.initialize",
                    _voiceReference);

            _voiceSupported = status.Supported;
            _voiceStatus = status.Message;
        }

        if (_needsStorageCheck)
        {
            _needsStorageCheck = false;

            var savedSelection =
                await SelectionStorage.GetAsync();

            string aircraftKey;
            string scenarioType;

            if (savedSelection?.IsValid == true &&
                int.TryParse(savedSelection.AircraftKey, out _))
            {
                aircraftKey = savedSelection.AircraftKey;
                scenarioType = savedSelection.ScenarioType;
            }
            else
            {
                var defaultAircraft =
                    _aircraftOptions.FirstOrDefault();

                var defaultScenario =
                    _scenarioRecords.FirstOrDefault();

                if (defaultAircraft is null ||
                    defaultScenario is null)
                {
                    _loadFailed = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                aircraftKey = defaultAircraft.Id.ToString();
                scenarioType = defaultScenario.EmergencyType;
            }

            await LoadSelectionAsync(
                aircraftKey,
                scenarioType,
                saveSelection: true);
        }

        if (firstRender || _isReady || _loadFailed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadSelectionAsync(
        string aircraftKey,
        string scenarioType,
        bool saveSelection = false)
    {
        try
        {
            if (!int.TryParse(aircraftKey, out var aircraftId))
            {
                throw new InvalidOperationException(
                    $"'{aircraftKey}' is not a valid aircraft identifier.");
            }

            var requestedAircraft =
                await AircraftService.GetByIdWithLandingGearAsync(aircraftId);

            if (requestedAircraft is null)
            {
                requestedAircraft = _aircraftOptions.FirstOrDefault() // Gracefully defaulting when Server Conflicts with Local 
                    ?? throw new KeyNotFoundException(
                        "No aircraft are available to simulate.");
            }

            selectedAircraft = requestedAircraft;

            cockpitLayout =
                await LayoutProvider.GetLayout(
                    selectedAircraft.CockpitLayoutKey);

            // Airspeed Layout Catch for Bad Data
            if (cockpitLayout.Airspeed.MaximumSpeed <=
                cockpitLayout.Airspeed.MinimumSpeed)
            {
                cockpitLayout.Airspeed =
                    new AirspeedIndicatorLayout
                    {
                        MinimumSpeed = 0,
                        MaximumSpeed = 200,
                        MinAirspeedAngle = -120,
                        MaxAirspeedAngle = 120,

                        WhiteArcStart = 40,
                        WhiteArcEnd = 85,

                        GreenArcStart = 48,
                        GreenArcEnd = 129,

                        YellowArcStart = 129,
                        YellowArcEnd = 163,

                        NeverExceedSpeed = 163
                    };
            }
            
            Console.WriteLine(
                $"Airspeed Layout: " +
                $"Min={cockpitLayout.Airspeed.MinimumSpeed}, " +
                $"Max={cockpitLayout.Airspeed.MaximumSpeed}, " +
                $"White={cockpitLayout.Airspeed.WhiteArcStart}-{cockpitLayout.Airspeed.WhiteArcEnd}, " +
                $"Green={cockpitLayout.Airspeed.GreenArcStart}-{cockpitLayout.Airspeed.GreenArcEnd}, " +
                $"Yellow={cockpitLayout.Airspeed.YellowArcStart}-{cockpitLayout.Airspeed.YellowArcEnd}, " +
                $"Vne={cockpitLayout.Airspeed.NeverExceedSpeed}");

            cockpitLayout.EngineCount = selectedAircraft.EngineCount;

            selectedScenarioRecord =
                FindScenarioRecord(scenarioType)
                ?? _scenarioRecords.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "No emergency scenarios are available.");

            selectedRuntimeScenario =
                SimulationEngine.GetScenario(
                    selectedScenarioRecord.EmergencyType);

            await InitializeSimulationStateAsync();

            // The resolved selection may differ from what was requested
            // (stale id fell back to a default) — persist and reflect
            // that in the URL so we don't keep re-triggering this fallback.
            if (saveSelection)
            {
                await SaveCurrentSelectionAsync();
                UpdateSimulationUrl();
            }
        }
        catch (Exception ex)
        {
            _loadFailed = true;
            _isReady = false;
            Console.WriteLine($"LoadSelectionAsync failed: {ex}");
        }
    }

    private EmergencyScenario?
        FindScenarioRecord(
            string scenarioValue)
    {
        var decodedValue =
            Uri.UnescapeDataString(
                scenarioValue);

        return _scenarioRecords.FirstOrDefault(
            scenario =>
                scenario.EmergencyType.Equals(
                    decodedValue,
                    StringComparison.OrdinalIgnoreCase) ||
                scenario.Title.Equals(
                    decodedValue,
                    StringComparison.OrdinalIgnoreCase));
    }

/* ====================================================================================================
 |                                     Aircraft/Scenario Menu                                          |
 ====================================================================================================== */

    private void ToggleAircraftMenu()
    {
        _isAircraftMenuOpen =
            !_isAircraftMenuOpen;

        _isScenarioMenuOpen = false;
    }

    private void ToggleScenarioMenu()
    {
        _isScenarioMenuOpen =
            !_isScenarioMenuOpen;

        _isAircraftMenuOpen = false;
    }

    private void CloseSelectorMenus()
    {
        _isAircraftMenuOpen = false;
        _isScenarioMenuOpen = false;
    }

    private async Task ChangeAircraftAsync(
        Aircraft aircraft)
    {
        CloseSelectorMenus();

        if (aircraft.Id ==
            selectedAircraft.Id)
        {
            return;
        }

        selectedAircraft = aircraft;

        cockpitLayout =
            await LayoutProvider.GetLayout(
                aircraft.CockpitLayoutKey);

        // Aircraft.EngineCount is the source of truth, not the layout's.
        cockpitLayout.EngineCount = selectedAircraft.EngineCount;

        await InitializeSimulationStateAsync();
        await SaveCurrentSelectionAsync();

        UpdateSimulationUrl();
    }

    private async Task ChangeScenarioAsync(
        EmergencyScenario scenario)
    {
        CloseSelectorMenus();

        if (scenario.Id ==
            selectedScenarioRecord.Id)
        {
            return;
        }

        selectedScenarioRecord = scenario;

        try
        {
            selectedRuntimeScenario =
                SimulationEngine.GetScenario(
                    scenario.EmergencyType);
        }
        catch
        {
            _loadFailed = true;
            _isReady = false;
            return;
        }

        ArmSelectedScenario();

        await SaveCurrentSelectionAsync();

        UpdateSimulationUrl();
    }

    private async Task SaveCurrentSelectionAsync()
    {
        await SelectionStorage.SaveAsync(
            new SimulationSelectionModel
            {
                AircraftKey =
                    selectedAircraft.Id.ToString(),

                ScenarioType =
                    selectedScenarioRecord.EmergencyType
            });
    }

/* ====================================================================================================
 |                                      Simulation Specific                                            |
 ====================================================================================================== */

    private void UpdateSimulationUrl()
    {
        var targetUrl = $"/simulation/{Uri.EscapeDataString(selectedAircraft.Id.ToString())}/{Uri.EscapeDataString(selectedScenarioRecord.EmergencyType)}";

        if (string.Equals(Navigation.Uri, Navigation.ToAbsoluteUri(targetUrl).ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Navigation.NavigateTo(targetUrl, replace: true);
    }

    private Task InitializeSimulationStateAsync()
    {
        simulationStartedAt = DateTime.UtcNow;
        emergencyTriggered = false;
        manualTriggerRequested = false;
        _emergencyModalHasBeenShown = false;
        _showEmergencyModal = false;

        procedureSteps =
            SimulationEngine.GetProcedureSteps(
                selectedScenarioRecord.EmergencyType,
                cockpitLayout,
                selectedScenarioRecord.Id);

        foreach (var step in procedureSteps)
        {
            step.MaxResponseSeconds =
                Math.Min(
                    selectedScenarioRecord.TimeLimitSeconds,
                    Math.Max(
                        step.MaxResponseSeconds,
                        10 + ((step.StepOrder - 1) * 15)));
        }

        // Build the complete aircraft state first.
        cockpitState =
            CreateNormalCockpitState();

        // The service keeps this state for delayed scenarios and applies
        // the emergency immediately when TriggerType is "Immediate".
        cockpitState =
            SimulationSession.StartSimulation(
                userId: "test-pilot",
                aircraftId: selectedAircraft.Id,
                scenario: selectedScenarioRecord,
                aircraft: cockpitLayout,
                expectedSteps: procedureSteps,
                initialState: cockpitState,
                pilotName: "Pilot");

        _completedReport = null;
        _isCompleting = false;
        _remainingSeconds =
            selectedScenarioRecord.TimeLimitSeconds;

        _completedProcedureStepOrders.Clear();

        cockpitState.DisplayedVerticalSpeed =
            cockpitState.VerticalSpeed;

        // An immediate scenario was already activated by StartSimulation.
        _showEmergencyModal =
            emergencyTriggered;

        _isReady = true;
        _loadFailed = false;

        CloseSelectorMenus();

        if (_simulationLoop is null)
        {
            StartSimulationLoop();
        }

        return Task.CompletedTask;
    }


/* ====================================================================================================
 |                                Procedure Checklist Management                                       |
 ===================================================================================================== */

    private bool IsProcedureStepCompleted(
        ScenarioProcedureStep step)
    {
        return _completedProcedureStepOrders.Contains(
            step.StepOrder);
    }

    private string GetChecklistProgressStyle()
    {
        return FormattableString.Invariant(
            $"width: {ChecklistProgressPercent:0.##}%;");
    }

    private string GetObjectiveText()
    {
        var firstStep =
            procedureSteps
                .OrderBy(step =>
                    step.StepOrder)
                .FirstOrDefault();

        if (firstStep is not null)
        {
            return firstStep.Instruction;
        }

        if (!string.IsNullOrWhiteSpace(
            selectedScenarioRecord.ExpectedProcedure))
        {
            return selectedScenarioRecord
                .ExpectedProcedure;
        }

        return selectedScenarioRecord.Description;
    }

    private static string FormatHeading(
        double heading)
    {
        var normalizedHeading =
            ((int)Math.Round(heading) % 360 + 360) % 360;

        return normalizedHeading
            .ToString("000");
    }

/* ====================================================================================================
 |                                  Cockpit State Management                                          |
 ===================================================================================================== */

    private CockpitState CreateNormalCockpitState()
    {
        var defaults = cockpitLayout.DefaultState;

        var engineCount = cockpitLayout.EngineCount > 0
            ? cockpitLayout.EngineCount
            : 1;

        var engines = Enumerable.Range(1, engineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = 0,
                Running = false,
                OnFire = false,
                EngineFire = false,
                FuelCutoff = false,
                FireSuppressionActivated = false,
                FuelPercentage = defaults.FuelPercentage
            })
            .ToList();

        var brakes = Enumerable.Range(1, selectedAircraft.BrakeCount)
            .Select(number => new BrakePressureState
            {
                Number = number,
                Pressure = 0
            })
            .ToList();

        var fuelTanks = Enumerable.Range(1, selectedAircraft.FuelTankCount)
            .Select(number => new FuelState
            {
                Number = number,
                Quantity = 26.5
            })
            .ToList();

        return new CockpitState
        {
            Airspeed = 0,
            Altitude = 0,
            Heading = defaults.DefaultHeading,

            VerticalSpeed = 0,
            DisplayedVerticalSpeed = 0,

            Pitch = 0,
            Bank = 0,

            FlightPhase = "Ground",

            Engines = engines,
            Brakes = brakes,
            FuelTanks = fuelTanks,

            FuelPercentage = defaults.FuelPercentage,

            AlertMessage = "Systems Normal",

            HydraulicPressure = 3000,
            HydraulicPumpOnline = true,
            HydraulicFault = false,

            OilPressure = 70,
            OilTemperature = 180
        };
    }
    private void UpdateFlightPhase()
    {
        cockpitState.FlightPhase =
            DetermineFlightPhase(
                cockpitState.Altitude,
                cockpitState.Airspeed,
                cockpitState.VerticalSpeed);
    }
    private static string DetermineFlightPhase(
        double altitude,
        double airspeed,
        double verticalSpeed)
    {
        if (altitude <= 100 && airspeed < 5)
        {
            return "Ground";
        }
            
        if (altitude <= 100 && airspeed < 40)
        {
            return "Taxi";
        }

        if (altitude <= 100 && airspeed >= 40)
        {
            return "Takeoff Roll";
        }

        if (altitude < 1_500 &&
            verticalSpeed > 300 &&
            airspeed >= 40)
        {
            return "Take-off";
        }

        if (verticalSpeed > 300)
        {
            return "Climb";
        }

        if (altitude <= 500 &&
            verticalSpeed <= 0 &&
            airspeed > 40)
        {
            return "Landing";
        }

        if (altitude <= 3_000 &&
            altitude > 500 &&
            verticalSpeed < 0)
        {
            return "Approach";
        }

        if (verticalSpeed < -300)
        {
            return "Descent";
        }

        return "Cruise";
    }

    private void UpdatePerformance(double elapsedSeconds)
    {
        var engineCount = cockpitState.Engines.Count;
        if (engineCount == 0)
        {
            return;
        }
        if (cockpitState.Altitude <= 0)
        {
            isOnGround = true;
        }
        else
        {
            isOnGround = false;
        }

        var averagePower = cockpitState.Engines.Average(engine => engine.Power);
        var powerPct = Math.Clamp(averagePower / 100.0, 0, 1);
        var pitchPct = Math.Clamp(cockpitState.Pitch / 15.0, -1, 1);

        var baseTargetSpeed = 20 + (powerPct * 90);
        var pitchDrag = cockpitState.Pitch * 1.2;
        var targetAirspeed = Math.Clamp(baseTargetSpeed - pitchDrag, 0, 160);

        var rudderInput = cockpitState.RudderPosition; // -1 to 1
        var brakeDiff = (cockpitState.BrakePressure) / 100.0;
        var yawInput = rudderInput + brakeDiff;

        var turnRate = 0.0;

        if (isOnGround)
        {
            turnRate = yawInput * (cockpitState.Airspeed < 20 ? 6 : 2);
        }
        else
        {
            var bankFactor = cockpitState.Bank / 30.0;
            turnRate = (bankFactor * 3.0) + (rudderInput * 1.5);
        }

        cockpitState.TurnRate = turnRate;
        cockpitState.Heading = NormalizeHeading(cockpitState.Heading + turnRate * elapsedSeconds);

        if (cockpitState.Altitude <= 0 && cockpitState.Airspeed <= 1)
        {
            if (cockpitState.Engines.All(e => e.Power <= 5))
            {
                cockpitState.Airspeed = 0;
                cockpitState.VerticalSpeed = 0;
                return;
            }
        }

        cockpitState.Airspeed = MoveToward(
            cockpitState.Airspeed,
            targetAirspeed,
            25 * elapsedSeconds);

        var vsiTarget =
            (pitchPct * 1200) +
            ((powerPct - 0.5) * 500) -
            ((cockpitState.Airspeed - targetAirspeed) * 2);

        cockpitState.VerticalSpeed = MoveToward(
            cockpitState.VerticalSpeed,
            vsiTarget,
            300 * elapsedSeconds);

        cockpitState.Altitude = Math.Max(
            0,
            cockpitState.Altitude + cockpitState.VerticalSpeed / 60.0 * elapsedSeconds);
    }
    private void UpdateFuelLeak(
        double elapsedSeconds)
    {
        if (!cockpitState.FuelLeakActive ||
            !cockpitState.LeakingFuelTankNumber.HasValue)
        {
            return;
        }

        var leakingTank =
            cockpitState.FuelTanks.FirstOrDefault(
                tank =>
                    tank.Number ==
                    cockpitState.LeakingFuelTankNumber.Value);

        if (leakingTank is null)
        {
            return;
        }

        const double leakGallonsPerSecond = 0.05;

        leakingTank.Quantity =
            Math.Max(
                0,
                leakingTank.Quantity -
                leakGallonsPerSecond *
                elapsedSeconds);

        cockpitState.FuelPercentage =
            CalculateFuelPercentage();
    }
    private double CalculateFuelPercentage()
    {
        if (cockpitState.FuelTanks.Count == 0)
        {
            return 0;
        }

        const double capacityPerTank = 26.5;

        var totalCapacity =
            cockpitState.FuelTanks.Count *
            capacityPerTank;

        var totalQuantity =
            cockpitState.FuelTanks.Sum(
                tank => tank.Quantity);

        return totalCapacity <= 0
            ? 0
            : Math.Clamp(
                totalQuantity /
                totalCapacity *
                100,
                0,
                100);
    }
    private async Task EvaluateCockpitStateProcedureStepAsync()
    {
        if (!emergencyTriggered ||
            _showEmergencyModal ||
            _completedReport is not null)
        {
            return;
        }

        var nextStep =
            procedureSteps
                .OrderBy(step => step.StepOrder)
                .FirstOrDefault(step =>
                    !_completedProcedureStepOrders.Contains(
                        step.StepOrder));

        if (nextStep is null ||
            nextStep.ValidationType !=
                ProcedureValidationType.CockpitState)
        {
            return;
        }

        if (!selectedRuntimeScenario.IsStepSatisfied(
                cockpitState,
                nextStep.StepOrder))
        {
            return;
        }

        // Record this as a successful state-based procedure step.
        cockpitState =
            SimulationSession.RecordStateStepCompletion(
                nextStep,
                cockpitState);

        _completedProcedureStepOrders.Add(
            nextStep.StepOrder);

        _latestInstructorFeedback =
            new AiInstructorFeedback
            {
                Severity = "Success",
                Message =
                    $"Procedure step completed: " +
                    $"{nextStep.Instruction}"
            };

        if (CompletedStepCount ==
            procedureSteps.Count)
        {
            await CompleteAssessmentAsync(
                "All procedure steps were completed.");
        }
    }


/* ====================================================================================================
 |                                       Emergency Trigger                                             |
 ===================================================================================================== */

    private void EvaluateEmergencyTrigger(
        string? pilotAction = null)
    {
        if (emergencyTriggered ||
            _showEmergencyModal ||
            _emergencyModalHasBeenShown ||
            !_isReady)
        {
            return;
        }

        var startCondition =
            selectedRuntimeScenario.StartCondition;

        if (!ScenarioStartConditionEvaluator.IsSatisfied(
                startCondition,
                cockpitState,
                selectedAircraft))
        {
            return;
        }

        var elapsed =
            DateTime.UtcNow -
            simulationStartedAt;

        var triggerSatisfied =
            TriggerEvaluator.ShouldTrigger(
                selectedScenarioRecord,
                cockpitState,
                elapsed,
                manualTriggerRequested,
                pilotAction);

        if (!triggerSatisfied)
        {
            return;
        }

        _emergencyModalHasBeenShown = true;
        _showEmergencyModal = true;
    }

    private void ActivateEmergencyScenario()
    {
        if (emergencyTriggered)
        {
            return;
        }

        emergencyTriggered = true;

        cockpitState =
            SimulationSession.MarkEmergencyTriggered(
                cockpitState);

        cockpitState.DisplayedVerticalSpeed =
            cockpitState.VerticalSpeed;

        if (selectedScenarioRecord.TimeLimitSeconds > 0)
        {
            _remainingSeconds =
                selectedScenarioRecord.TimeLimitSeconds;
        }
        else
        {
            _remainingSeconds = 0;
        }

        _showEmergencyModal = false;
    }
    private void DismissEmergencyModal()
    {
        _showEmergencyModal = false;
    }
    private void ActivateEmergencyManually()
    {
        if (emergencyTriggered ||
            !string.Equals(
                selectedScenarioRecord.TriggerType,
                "Manual",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        manualTriggerRequested = true;
        EvaluateEmergencyTrigger();
    }

    private string GetTriggerStatusText()
    {
        return selectedScenarioRecord.TriggerType switch
        {
            "Time" when selectedScenarioRecord.TriggerDelaySeconds.HasValue =>
                $"Emergency will activate after " +
                $"{selectedScenarioRecord.TriggerDelaySeconds.Value} seconds.",

            "Altitude" when selectedScenarioRecord.TriggerAltitudeFeet.HasValue =>
                $"Emergency will activate at " +
                $"{selectedScenarioRecord.TriggerAltitudeFeet.Value:N0} feet.",

            "Airspeed" when selectedScenarioRecord.TriggerAirspeedKnots.HasValue =>
                $"Emergency will activate at " +
                $"{selectedScenarioRecord.TriggerAirspeedKnots.Value:N0} knots.",

            "Flight Phase" when !string.IsNullOrWhiteSpace(
                selectedScenarioRecord.TriggerFlightPhase) =>
                $"Emergency will activate during " +
                $"{selectedScenarioRecord.TriggerFlightPhase}.",

            "Manual" =>
                "Waiting for instructor activation.",

            _ =>
                "Waiting for the configured emergency trigger."
        };
    }
    private FireDetectionStatus GetFireStatus()
    {
        var engine = GetAffectedEngine();
        if (engine is null)
            return FireDetectionStatus.Normal;

        if (engine.FireSuppressionActivated)
            return (engine.EngineFire || engine.OnFire)
                ? FireDetectionStatus.Suppressed
                : FireDetectionStatus.Extinguished;

        if (engine.EngineFire || engine.OnFire)
            return FireDetectionStatus.Warning;

        if (selectedScenarioRecord.EmergencyType == "Smoke or Fire")
            return FireDetectionStatus.Caution;

        return FireDetectionStatus.Normal;
    }

/* ====================================================================================================
 |                               Simulation Loop and Completion                                        |
 ===================================================================================================== */

    private void StartSimulationLoop()
    {
        _simulationCancellation =
            new CancellationTokenSource();

        _simulationTimer =
            new PeriodicTimer(
                TimeSpan.FromMilliseconds(100));

        _simulationLoop =
            RunSimulationLoopAsync(
                _simulationCancellation.Token);
    }

    private async Task RunSimulationLoopAsync(
        CancellationToken cancellationToken)
    {
        const double elapsedSeconds = 0.1;

        try
        {
            while (_simulationTimer is not null &&
                   await _simulationTimer
                       .WaitForNextTickAsync(
                           cancellationToken))
            {
                cockpitState.DisplayedVerticalSpeed =
                    VSIMath.ApplyLag(
                        cockpitState.DisplayedVerticalSpeed,
                        cockpitState.VerticalSpeed,
                        elapsedSeconds,
                        cockpitLayout.VSI.LagSeconds);

                UpdatePerformance(elapsedSeconds);
                UpdateFlightPhase();

                EvaluateEmergencyTrigger();

                if (emergencyTriggered)
                {
                    await EvaluateCockpitStateProcedureStepAsync();
                }
                
                if (emergencyTriggered &&
                    _completedReport is null &&
                    selectedScenarioRecord.TimeLimitSeconds > 0)
                {
                    _remainingSeconds =
                        SimulationSession.GetRemainingSeconds();

                    if (SimulationSession.IsTimedOut())
                    {
                        await CompleteAssessmentAsync(
                            "The configured scenario time limit expired.");
                    }
                }

                await InvokeAsync(
                    StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when leaving the page.
        }
    }

    private void ArmSelectedScenario()
    {
        simulationStartedAt = DateTime.UtcNow;

        emergencyTriggered = false;
        manualTriggerRequested = false;
        _emergencyModalHasBeenShown = false;
        _showEmergencyModal = false;

        _completedReport = null;
        _isCompleting = false;

        _completedProcedureStepOrders.Clear();

        procedureSteps =
            SimulationEngine.GetProcedureSteps(
                selectedScenarioRecord.EmergencyType,
                cockpitLayout,
                selectedScenarioRecord.Id);

        foreach (var step in procedureSteps)
        {
            step.MaxResponseSeconds =
                Math.Min(
                    selectedScenarioRecord.TimeLimitSeconds,
                    Math.Max(
                        step.MaxResponseSeconds,
                        10 + ((step.StepOrder - 1) * 15)));
        }

        _remainingSeconds =
            selectedScenarioRecord.TimeLimitSeconds;

        // Register the selected scenario with the service,
        // but pass the CURRENT aircraft state.
        cockpitState =
            SimulationSession.StartSimulation(
                userId: "test-pilot",
                aircraftId: selectedAircraft.Id,
                scenario: selectedScenarioRecord,
                aircraft: cockpitLayout,
                expectedSteps: procedureSteps,
                initialState: cockpitState,
                pilotName: "Pilot");
    }

    private Task CompleteAssessmentAsync()
    {
        return CompleteAssessmentAsync(null);
    }

    private async Task CompleteAssessmentAsync(
        string? completionReason)
    {
        if (_completedReport is not null ||
            _isCompleting ||
            !emergencyTriggered)
        {
            return;
        }

        try
        {
            _isCompleting = true;

            _completedReport =
                await SimulationSession
                    .CompleteAndSaveSimulationAsync(
                        completionReason);
        }
        finally
        {
            _isCompleting = false;
        }
    }
    private async Task HandlePilotActionAsync(
        string actionName)
    {
        if (!_isReady ||
            _completedReport is not null)
        {
            return;
        }

        if (!emergencyTriggered)
        {
            EvaluateEmergencyTrigger(
                actionName);

            return;
        }

        var nextExpectedStep =
            procedureSteps
                .OrderBy(step => step.StepOrder)
                .FirstOrDefault(step =>
                    !_completedProcedureStepOrders.Contains(
                        step.StepOrder));
        if (nextExpectedStep is not null &&
            nextExpectedStep.ValidationType ==
                ProcedureValidationType.CockpitState)
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Information",
                    Message =
                        $"Complete the current procedure step first: " +
                        $"{nextExpectedStep.Instruction}"
                };

            return;
        }

        var matchingStep =
            procedureSteps.FirstOrDefault(
                step =>
                    step.ValidationType ==
                        ProcedureValidationType.PilotAction &&
                    string.Equals(
                        step.CorrectAction,
                        actionName,
                        StringComparison.OrdinalIgnoreCase));

        var selectedOrder =
            matchingStep?.StepOrder ??
            Math.Max(
                1,
                CompletedStepCount + 1);

        cockpitState =
            SimulationSession.SubmitPilotAction(
                actionName,
                selectedOrder);

        var recordedAction =
            SimulationSession.PilotActions.LastOrDefault();

        if (recordedAction is null)
        {
            return;
        }

        if (recordedAction.WasCorrect &&
            recordedAction.WasInCorrectOrder)
        {
            if (recordedAction.ExpectedStepOrder.HasValue)
            {
                _completedProcedureStepOrders.Add(
                    recordedAction.ExpectedStepOrder.Value);
            }

            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity =
                        recordedAction.WasWithinTimeLimit
                            ? "Success"
                            : "Warning",

                    Message =
                        recordedAction.WasWithinTimeLimit
                            ? $"Correct action: {actionName}."
                            : $"Correct action, but performed late: {actionName}."
                };
        }
        else if (recordedAction.WasCorrect)
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Warning",

                    Message =
                        $"'{actionName}' is part of the procedure, " +
                        "but it was performed out of sequence.",

                    RecommendedAction =
                        GetNextExpectedInstruction()
                };
        }
        else
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Warning",

                    Message =
                        $"'{actionName}' is not the expected procedure action.",

                    RecommendedAction =
                        GetNextExpectedInstruction()
                };
        }

        if (CompletedStepCount ==
            procedureSteps.Count &&
            procedureSteps.Count > 0)
        {
            await CompleteAssessmentAsync(
                "All procedure steps were completed.");
        }

        await InvokeAsync(
            StateHasChanged);
    }
    private string? GetNextExpectedInstruction()
    {
        return procedureSteps
            .OrderBy(step => step.StepOrder)
            .FirstOrDefault(step =>
                !_completedProcedureStepOrders.Contains(
                    step.StepOrder))
            ?.Instruction;
    }


    private async Task ToggleVoiceControlAsync()
    {
        if (!_voiceSupported)
        {
            return;
        }

        _voiceListening = !_voiceListening;

        if (_voiceListening)
        {
            var started =
                await JSRuntime.InvokeAsync<bool>(
                    "aeroVoice.start");

            _voiceListening = started;
            _voiceStatus = started
                ? "Listening for cockpit commands."
                : "Voice recognition could not start.";
        }
        else
        {
            await JSRuntime.InvokeVoidAsync(
                "aeroVoice.stop");

            _voiceStatus = "Voice control stopped.";
        }
    }

    [JSInvokable]
    public async Task ReceiveVoiceTranscript(
        string transcript,
        double confidence)
    {
        _lastVoiceTranscript = transcript;

        if (!_isReady ||
            !emergencyTriggered ||
            _completedReport is not null)
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Information",
                    Message =
                        "The emergency assessment must be active before " +
                        "commands can be processed."
                };

            await InvokeAsync(StateHasChanged);
            return;
        }

        var request =
            CockpitCommands.Parse(
                transcript,
                cockpitLayout);

        if (request is null)
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Warning",
                    Message =
                        $"I could not match '{transcript}' to an " +
                        "available cockpit control.",
                    RecommendedAction =
                        "Use the instrument name, action and value."
                };

            await JSRuntime.InvokeVoidAsync(
                "aeroVoice.speak",
                _latestInstructorFeedback.Message);

            await InvokeAsync(StateHasChanged);
            return;
        }

        var result =
            CockpitCommands.Execute(
                request,
                cockpitLayout,
                cockpitState);

        if (result.Succeeded)
        {
            var matchedStep =
                procedureSteps.FirstOrDefault(
                    step =>
                        step.ValidationType ==
                            ProcedureValidationType.PilotAction &&
                        step.CorrectAction.Equals(
                            result.ActionName,
                            StringComparison.OrdinalIgnoreCase));

            var selectedOrder =
                matchedStep?.StepOrder ??
                Math.Max(
                    1,
                    CompletedStepCount + 1);

            var nextExpectedStep =
                procedureSteps
                    .OrderBy(step => step.StepOrder)
                    .FirstOrDefault(step =>
                        !_completedProcedureStepOrders.Contains(
                            step.StepOrder));
            
            if (nextExpectedStep is not null &&
                nextExpectedStep.ValidationType ==
                    ProcedureValidationType.CockpitState)
            {
                _latestInstructorFeedback =
                    new AiInstructorFeedback
                    {
                        Severity = "Information",
                        Message =
                            $"Complete the current procedure step first: " +
                            $"{nextExpectedStep.Instruction}"
                    };

                return;
            }

            cockpitState =
                SimulationSession.RecordPilotAction(
                    result.ActionName,
                    selectedOrder,
                    cockpitState);

            var recordedAction =
                SimulationSession.PilotActions.LastOrDefault();

            if (recordedAction is
                {
                    WasCorrect: true,
                    WasInCorrectOrder: true,
                    ExpectedStepOrder: not null
                })
            {
                _completedProcedureStepOrders.Add(
                    recordedAction.ExpectedStepOrder.Value);
            }
        }

        _latestInstructorFeedback =
            AiInstructor.EvaluateAction(
                result,
                procedureSteps,
                SimulationSession.PilotActions,
                _remainingSeconds);

        _voiceStatus =
            result.SpokenFeedback;

        await JSRuntime.InvokeVoidAsync(
            "aeroVoice.speak",
            _latestInstructorFeedback.Message);

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task VoiceRecognitionError(string error)
    {
        _voiceListening = false;
        _voiceStatus =
            $"Voice recognition error: {error}";

        await InvokeAsync(StateHasChanged);
    }

    private sealed class VoiceInitializationResult
    {
        public bool Supported { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    private static string FormatAssessmentTime(int totalSeconds)
    {
        var safeSeconds = Math.Max(0, totalSeconds);
        return TimeSpan.FromSeconds(safeSeconds).ToString(@"mm\:ss");
    }

    public async ValueTask DisposeAsync()
    {
        _simulationCancellation?.Cancel();

        if (_simulationLoop is not null)
        {
            try
            {
                await _simulationLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal.
            }
        }

        if (_voiceReference is not null)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync(
                    "aeroVoice.dispose");
            }
            catch (JSDisconnectedException)
            {
                // Expected if the browser circuit has already closed.
            }
            catch (InvalidOperationException)
            {
                // JS interop may be unavailable during prerender disposal.
            }

            _voiceReference.Dispose();
            _voiceReference = null;
        }
        _voiceReference?.Dispose();
        _simulationTimer?.Dispose();
        _simulationCancellation?.Dispose();
    }

/* ====================================================================================================
 |                                     Instrument Management                                           |
 ===================================================================================================== */

    private async Task HandleUnitClick(LandingGearUnit unit)
    {
        var wasUp = unit.Status == LandingGearStatusValue.UpAndLocked;

        unit.Status = LandingGearStatusValue.Moving;
        await InvokeAsync(StateHasChanged);

        await Task.Delay(1500);

        if (selectedScenarioRecord.EmergencyType == "Landing Gear Malfunction")
        {
            unit.Status = LandingGearStatusValue.Unsafe;
        }
        else
        {
            unit.Status = wasUp
                ? LandingGearStatusValue.DownAndLocked
                : LandingGearStatusValue.UpAndLocked;
        }

        await InvokeAsync(StateHasChanged);
    }
    private EngineState? GetAffectedEngine()
    {
        return cockpitState.Engines.FirstOrDefault(e => e.EngineFire || e.OnFire)
            ?? cockpitState.Engines.FirstOrDefault(e => e.Number == 2)
            ?? cockpitState.Engines.FirstOrDefault();
    }
    private async Task HandleEngineStatusFocusAsync(
        EngineState engine)
    {
        if (!emergencyTriggered)
        {
            return;
        }

        var nextStep =
            procedureSteps
                .OrderBy(step => step.StepOrder)
                .FirstOrDefault(step =>
                    !_completedProcedureStepOrders.Contains(
                        step.StepOrder));

        if (nextStep is null ||
            !string.Equals(
                nextStep.CorrectAction,
                "Check Engine Status",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await HandlePilotActionAsync(
            "Check Engine Status");
    }
    private async Task ActivateFireSuppression()
    {
        var engine = GetAffectedEngine();
        if (engine is null)
            return;

        engine.FireSuppressionActivated = true;
        await InvokeAsync(StateHasChanged);

        await Task.Delay(1500);

        if (selectedScenarioRecord.EmergencyType == "Engine Fire")
        {
            var fireStillActive = engine.EngineFire || engine.OnFire;
            engine.EngineFire = fireStillActive;
            engine.OnFire = fireStillActive;
        }

        await InvokeAsync(StateHasChanged);
    }
    private static double MoveToward(double current, double target, double maxDelta)
    {
        if (current < target)
        {
            return Math.Min(current + maxDelta, target);
        }

        return Math.Max(current - maxDelta, target);
    }

    private static double NormalizeHeading(double heading)
    {
        heading %= 360;
        if (heading < 0)
        {
            heading += 360;
        }

        return heading;
    }
    private void ToggleFuelControl(
        EngineState engine)
    {
        engine.FuelCutoff =
            !engine.FuelCutoff;

        if (engine.FuelCutoff)
        {
            engine.Running = false;
        }
        else
        {
            engine.Running = true;
        }
    }
    private void HandleThrottleChanged(
        EngineState engine,
        double power)
    {
        engine.Power = power;

        if (power > 0 &&
            !engine.FuelCutoff)
        {
            engine.Running = true;
        }

        if (power <= 0)
        {
            engine.Power = 0;
        }
    }
    private void HandleFuelControlChanged(
    EngineState engine)
    {
        engine.FuelCutoff =
            !engine.FuelCutoff;

        if (engine.FuelCutoff)
        {
            engine.Running = false;
        }
        else
        {
            engine.Running = true;
        }
    }
    private void HandleRadioPower()
    {
        cockpitState.RadioPowered =
            !cockpitState.RadioPowered;

        if (!cockpitState.RadioPowered)
        {
            cockpitState.RadioTransmitting = false;
        }
    }
    private void HandleGuardFrequency()
    {
        if (!cockpitState.RadioPowered)
        {
            return;
        }

        cockpitState.RadioFrequency = 121.5;
    }


    private async Task HandleRadioTransmitAsync()
    {
        if (!cockpitState.RadioPowered)
        {
            return;
        }

        cockpitState.RadioTransmitting = true;

        await InvokeAsync(StateHasChanged);

        var nextStep =
            procedureSteps
                .OrderBy(step => step.StepOrder)
                .FirstOrDefault(step =>
                    !_completedProcedureStepOrders.Contains(
                        step.StepOrder));

        if (nextStep is not null &&
            string.Equals(
                nextStep.CorrectAction,
                "Transmit Emergency",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                "Transmit Emergency");
        }

        await Task.Delay(750);

        cockpitState.RadioTransmitting = false;
    }


    private void HandleSatellitePower()
    {
        cockpitState.SatellitePhonePowered =
            !cockpitState.SatellitePhonePowered;

        if (!cockpitState.SatellitePhonePowered)
        {
            cockpitState.SatellitePhoneConnected = false;
        }
    }


    private void HandleSatelliteConnection()
    {
        if (!cockpitState.SatellitePhonePowered)
        {
            return;
        }

        cockpitState.SatellitePhoneConnected =
            !cockpitState.SatellitePhoneConnected;
    }


    private async Task HandleSatelliteEmergency()
    {
        if (!cockpitState.SatellitePhoneConnected)
        {
            return;
        }

        cockpitState.CommunicationStatus =
            "Emergency message transmitted by satellite.";

        await HandlePilotActionAsync(
            "Declare Emergency");
    }
    /* ====================================================================================================
     |                                          Debug Controls                                              |
     ===================================================================================================== */
     private void DebugSetCruiseAltitude()
    {
        cockpitState.Altitude = 12_000;
        cockpitState.Airspeed = 110;
        cockpitState.VerticalSpeed = 0;
        cockpitState.DisplayedVerticalSpeed = 0;
        cockpitState.Pitch = 0;
        cockpitState.Bank = 0;
        cockpitState.FlightPhase = "Cruise";

        foreach (var engine in cockpitState.Engines)
        {
            engine.Running = true;
            engine.Power = 75;
        }
    }
}