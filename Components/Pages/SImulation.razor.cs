using System.Security.Claims;
using AeroResponse.Models;
using AeroResponse.Services;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Controls;
using AeroResponse.Simulation.Layouts;
using AeroResponse.Simulation.Scenarios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using AeroResponse.Services.Authorization;

using SimulationSelectionModel =
    AeroResponse.Models.SimulationSelection;

using VSIMath =
    AeroResponse.Simulation.Instruments
        .VerticalSpeedIndicator.VSIMath;

namespace AeroResponse.Components.Pages;

public partial class Simulation : ComponentBase, IAsyncDisposable
{
    /* ====================================================================================================
                                            Dependency Injection
       ==================================================================================================== */
    [Inject]
    private ILogger<Simulation> Logger { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider
    {
        get;
        set;
    } = default!;

    [Inject]
    private ICockpitLayoutProvider LayoutProvider { get; set; } = default!;

    [Inject]
    private SimulationEngine SimulationEngine { get; set; } = default!;

    [Inject]
    private SimulationSelectionStorage SelectionStorage { get; set; } = default!;

    [Inject]
    private SimulationScenarioDataService ScenarioDataService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private AircraftService AircraftService { get; set; } = default!;

    [Inject]
    private AircraftAccessService AircraftAccessService { get; set; } = default!;

    [Inject]
    private ScenarioTriggerEvaluator TriggerEvaluator { get; set; } = default!;

    [Inject]
    private SimulationService SimulationSession { get; set; } = default!;

    [Inject]
    private CockpitCommandService CockpitCommands { get; set; } = default!;

    [Inject]
    private AiInstructorService AiInstructor { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /* ====================================================================================================
                                            Variable Declaration
       ==================================================================================================== */

    private CockpitState cockpitState = new();
    private bool simulationStarted = false;
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

    private string _voiceStatus =
        "Select Start Voice Control.";

    private string? _lastVoiceTranscript;
    private bool _emergencyModalHasBeenShown = false;

    private string? _previousVoiceTranscript;

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

    private string _currentPilotUserId = "test-pilot";

    private string _currentPilotName =
        "Pilot";

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
                                        State Based Actions                                            |
     ====================================================================================================== */

    protected override async Task OnParametersSetAsync()
    {
        _isReady = false;
        _loadFailed = false;

        var authenticationState =
            await AuthenticationStateProvider
                .GetAuthenticationStateAsync();

        var principal =
            authenticationState.User;

        _currentPilotUserId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(
                ClaimTypes.Email)
            ?? principal.Identity?.Name
            ?? string.Empty;

        // Debug Until Authentication
        _currentPilotUserId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? "debug-pilot";


        if (string.IsNullOrWhiteSpace(
            _currentPilotUserId))
        {
            throw new InvalidOperationException(
                "Authenticated user does not have a usable identifier.");
        }

        var firstName =
            principal.FindFirstValue(
                ClaimTypes.GivenName);

        var surname =
            principal.FindFirstValue(
                ClaimTypes.Surname);

        var fullName =
            string.Join(
                " ",
                new[]
                {
                    firstName,
                    surname
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value)));

        _currentPilotName =
            !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : principal.Identity?.Name
                    ?? "Pilot";

        var allAircraft =
            await AircraftService.GetAllAsync();

        var availableLayoutKeys =
            (await LayoutProvider.GetLayouts())
                .Select(layout => layout.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var aircraftWithLayouts = allAircraft
            .Where(aircraft =>
                !string.IsNullOrWhiteSpace(
                    aircraft.CockpitLayoutKey) &&
                availableLayoutKeys.Contains(
                    aircraft.CockpitLayoutKey))
            .ToArray();

        _aircraftOptions =
            await AircraftAccessService.FilterAllowedAircraftAsync(
                principal,
                aircraftWithLayouts);

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
                _aircraftOptions.FirstOrDefault(
                    aircraft => aircraft.Id == aircraftId);

            if (requestedAircraft is null)
            {
                requestedAircraft = _aircraftOptions.FirstOrDefault()
                    ?? throw new KeyNotFoundException(
                        "No aircraft are available for this account.");
            }

            // Reload the allowed aircraft with its landing gear configuration.
            selectedAircraft =
                await AircraftService.GetByIdWithLandingGearAsync(requestedAircraft.Id)
                ?? requestedAircraft;

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

            Logger.LogError(
                ex,
                "Failed to load aircraft {AircraftKey} and scenario {ScenarioType}.",
                aircraftKey,
                scenarioType);
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
                                        Aircraft/Scenario Menu                                          |
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
                                    Simulation Specific                                            |
     ====================================================================================================== */

    private void ToggleFlightSimulation()
    {
        if (simulationStarted)
        {
            StopFlightSimulation();
        }
        else
        {
            StartFlightSimulation();
        }
    }

    private void StartFlightSimulation()
    {
        if (simulationStarted)
        {
            return;
        }

        simulationStarted = true;
        simulationStartedAt = DateTime.UtcNow;

        cockpitState.AlertMessage =
            "SIMULATION ACTIVE";
    }

    private void StopFlightSimulation()
    {
        if (!simulationStarted)
        {
            return;
        }

        simulationStarted = false;

        cockpitState.AlertMessage =
            "SIMULATION PAUSED";
    }

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
        simulationStarted = false;

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
                userId: _currentPilotUserId,
                aircraftId: selectedAircraft.Id,
                scenario: selectedScenarioRecord,
                aircraft: cockpitLayout,
                expectedSteps: procedureSteps,
                initialState: cockpitState,
                pilotName: _currentPilotName);

        _completedReport = null;
        _isCompleting = false;
        _remainingSeconds =
            selectedScenarioRecord.TimeLimitSeconds;

        _completedProcedureStepOrders.Clear();

        _previousVoiceTranscript = null;
        _lastVoiceTranscript = null;

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
                                    Procedure Checklist Management                                       |
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
                                    Cockpit State Management                                          |
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

        var landingGears =
            selectedAircraft.LandingGearConfig.Units
                .OrderBy(unit => unit.Order)
                .Select(unit => new LandingGearState
                {
                    Number = unit.Number,
                    Label = unit.Label,
                    Position = unit.Position,
                    Status = unit.Status
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
            LandingGears = landingGears,

            FuelPercentage = defaults.FuelPercentage,

            AlertMessage = "Systems Normal",

            AlternateGearExtensionActivated = false,
            AlternateGearExtensionCompleted = false,

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

        var baseTargetSpeed =
            GetTargetAirspeed();

        var pitchDrag =
            Math.Max(
                0,
                cockpitState.Pitch) *
            GetPitchDragFactor();

        var targetAirspeed =
            Math.Clamp(
                baseTargetSpeed - pitchDrag,
                0,
                cockpitLayout.Airspeed.MaximumSpeed);

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

        cockpitState.Airspeed =
            MoveToward(
                cockpitState.Airspeed,
                targetAirspeed,
                GetAirspeedResponseRate() *
                elapsedSeconds);

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
    private double GetAirspeedResponseRate()
    {
        var cruiseSpeed =
            selectedAircraft.CruiseSpeed;

        return cruiseSpeed switch
        {
            <= 150 => 4.0,
            <= 300 => 6.0,
            <= 400 => 8.0,
            _ => 10.0
        };
    }
    private double GetPitchDragFactor()
    {
        return selectedAircraft.CruiseSpeed switch
        {
            <= 150 => 1.2,
            <= 300 => 2.0,
            <= 400 => 2.5,
            _ => 3.0
        };
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
                                        Emergency Trigger                                             |
     ===================================================================================================== */

    private async void EvaluateEmergencyTrigger(
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

        await JSRuntime.InvokeVoidAsync(
            "aeroEmergencyAudio.playWarning");
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
                                Simulation Loop and Completion                                        |
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
                await _simulationTimer.WaitForNextTickAsync(
                    cancellationToken))
            {
                if (!simulationStarted)
                {
                    continue;
                }

                cockpitState.DisplayedVerticalSpeed =
                    VSIMath.ApplyLag(
                        cockpitState.DisplayedVerticalSpeed,
                        cockpitState.VerticalSpeed,
                        elapsedSeconds,
                        cockpitLayout.VSI.LagSeconds);

                UpdatePerformance(
                    elapsedSeconds);

                UpdateFlightPhase();

                EvaluateEmergencyTrigger();

                if (emergencyTriggered)
                {
                    UpdateFuelLeak(
                        elapsedSeconds);

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
                userId: _currentPilotUserId,
                aircraftId: selectedAircraft.Id,
                scenario: selectedScenarioRecord,
                aircraft: cockpitLayout,
                expectedSteps: procedureSteps,
                initialState: cockpitState,
                pilotName: _currentPilotName);
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

            Logger.LogInformation(
                "Completing simulation. UserId={UserId}, Aircraft={Aircraft}, Scenario={Scenario}",
                _currentPilotUserId,
                selectedAircraft.Name,
                selectedScenarioRecord.EmergencyType);

            _completedReport =
                await SimulationSession
                    .CompleteAndSaveSimulationAsync(
                        completionReason);

            Logger.LogInformation(
                "Simulation report saved. ReportId={ReportId}, UserId={UserId}",
                _completedReport.Id,
                _completedReport.UserId);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to complete and save simulation report.");

            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Warning",
                    Message =
                        $"The simulation completed, but the report could not be saved: " +
                        $"{ex.Message}"
                };
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
    private ScenarioProcedureStep?
    GetNextIncompleteProcedureStep()
    {
        return procedureSteps
            .OrderBy(step => step.StepOrder)
            .FirstOrDefault(step =>
                !_completedProcedureStepOrders.Contains(
                    step.StepOrder));
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
        transcript =
            transcript.Trim();

        if (string.IsNullOrWhiteSpace(
                transcript))
        {
            return;
        }

        if (string.Equals(
                transcript,
                _previousVoiceTranscript,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _previousVoiceTranscript =
            transcript;

        _lastVoiceTranscript =
            transcript;

        Logger.LogInformation(
            "Voice transcript received: {Transcript}",
            transcript);


        // =========================================================
        // SIMULATION LIFECYCLE
        // =========================================================

        if (string.Equals(
                transcript,
                "start simulation",
                StringComparison.OrdinalIgnoreCase))
        {
            StartFlightSimulation();

            _voiceStatus =
                "Simulation started.";

            await InvokeAsync(
                StateHasChanged);

            return;
        }


        if (!_isReady ||
            _completedReport is not null)
        {
            _voiceStatus =
                "Simulator unavailable.";

            return;
        }


        if (!simulationStarted)
        {
            _voiceStatus =
                "Start the simulation before operating controls.";

            return;
        }


        // =========================================================
        // COCKPIT CONTROL
        // =========================================================

        var result =
            await ExecuteVoiceCockpitCommandAsync(
                transcript);

        _voiceStatus =
            result.SpokenFeedback;

        _latestInstructorFeedback =
            new AiInstructorFeedback
            {
                Severity =
                    result.Succeeded
                        ? "Success"
                        : "Warning",

                Message =
                    result.SpokenFeedback
            };


        if (!string.IsNullOrWhiteSpace(
                result.SpokenFeedback))
        {
            await JSRuntime.InvokeVoidAsync(
                "aeroVoice.speak",
                result.SpokenFeedback);
        }

        await InvokeAsync(
            StateHasChanged);
    }
    [JSInvokable]
    public async Task VoiceRecognitionError(
        string error)
    {
        if (error is "no-speech" or "aborted")
        {
            _voiceStatus =
                "No command detected. Still listening.";

            await InvokeAsync(StateHasChanged);
            return;
        }

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

        _simulationTimer?.Dispose();
        _simulationCancellation?.Dispose();
    }

    /* ====================================================================================================
                                        Instrument Management                                           |
     ===================================================================================================== */

    private void SetPitch(
        double pitch)
    {
        cockpitState.Pitch =
            Math.Clamp(
                pitch,
                cockpitLayout.ArtificialHorizon.MinimumPitch,
                cockpitLayout.ArtificialHorizon.MaximumPitch);
    }
    private void ChangePitch(
        double delta)
    {
        SetPitch(
            cockpitState.Pitch + delta);
    }
    private void SetBank(
        double bank)
    {
        cockpitState.Bank =
            Math.Clamp(
                bank,
                cockpitLayout.ArtificialHorizon.MinimumBank,
                cockpitLayout.ArtificialHorizon.MaximumBank);
    }
    private void ChangeBank(
        double delta)
    {
        SetBank(
            cockpitState.Bank + delta);
    }
    private void SetRudderPosition(
        double position)
    {
        cockpitState.RudderPosition =
            Math.Clamp(
                position,
                -1,
                1);
    }
    private async Task HandleUnitClick(
        LandingGearState unit)
    {
        var shouldGoDown =
            unit.Status ==
            LandingGearStatusValue.UpAndLocked;

        await SetLandingGearPositionAsync(
            unit,
            shouldGoDown);

        await InvokeAsync(
            StateHasChanged);
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
    private async Task HandleHydraulicStatusFocusAsync()
    {
        if (!emergencyTriggered)
        {
            return;
        }

        var nextStep =
            GetNextIncompleteProcedureStep();

        if (nextStep is null ||
            !string.Equals(
                nextStep.CorrectAction,
                "Identify Hydraulic Failure",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await HandlePilotActionAsync(
            "Identify Hydraulic Failure");
    }
    private async Task HandleLandingGearStatusFocusAsync()
    {
        if (!emergencyTriggered)
        {
            return;
        }

        var expectedStep = GetNextIncompleteProcedureStep();

        if (expectedStep?.CorrectAction != "Check Gear Status")
        {
            return;
        }

        await HandlePilotActionAsync("Check Gear Status");
    }
    private async Task ActivateFireSuppression()
    {
        var engine =
            GetAffectedEngine();

        if (engine is null)
        {
            return;
        }

        await ActivateEngineFireSuppressionAsync(
            engine);
    }
    private void ActivateCabinFireSuppression()
    {
        cockpitState.FireSuppressionActivated = true;

        if (cockpitState.FireDetected)
        {
            cockpitState.FireDetected = false;

            cockpitState.AlertMessage =
                "FIRE SUPPRESSION ACTIVE - FIRE CONDITION SUPPRESSED";
        }
        else
        {
            cockpitState.AlertMessage =
                "FIRE SUPPRESSION ACTIVATED - NO ACTIVE FIRE DETECTED";
        }
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
    private void SetFuelCutoff(
        EngineState engine,
        bool cutoff)
    {
        engine.FuelCutoff =
            cutoff;

        if (cutoff)
        {
            engine.Power = 0;
            engine.Running = false;
        }
        else
        {
            engine.Running =
                engine.Power > 0;
        }
    }
    private bool SetFuelCutoff( // Voice Control
        int engineNumber,
        bool cutoff)
    {
        var engine =
            cockpitState.Engines
                .FirstOrDefault(
                    engine =>
                        engine.Number ==
                        engineNumber);

        if (engine is null)
        {
            return false;
        }

        SetFuelCutoff(
            engine,
            cutoff);

        return true;
    }
    private void SetEnginePower(
        EngineState engine,
        double power)
    {
        power =
            Math.Clamp(
                power,
                0,
                100);

        engine.Power =
            power;

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
    private bool SetEnginePower( // Voice Friendly Version
        int engineNumber,
        double power)
    {
        var engine =
            cockpitState.Engines
                .FirstOrDefault(
                    engine =>
                        engine.Number ==
                        engineNumber);

        if (engine is null)
        {
            return false;
        }

        SetEnginePower(
            engine,
            power);

        return true;
    }
    private void SetAllEnginePower(
        double power)
    {
        foreach (var engine in cockpitState.Engines)
        {
            SetEnginePower(
                engine,
                power);
        }
    }
    private void SetRadioPower(
        bool powered)
    {
        cockpitState.RadioPowered =
            powered;

        if (!powered)
        {
            cockpitState.RadioTransmitting =
                false;
        }
    }
    private void SetSatellitePower(
        bool powered)
    {
        cockpitState.SatellitePhonePowered =
            powered;

        if (!powered)
        {
            cockpitState.SatellitePhoneConnected =
                false;
        }
    }
    private void SetSatelliteConnection(
        bool connected)
    {
        if (!cockpitState.SatellitePhonePowered)
        {
            return;
        }

        cockpitState.SatellitePhoneConnected =
            connected;
    }
    private async void SatelliteEmergencyRequested(
        bool connected)
    {
        if (!cockpitState.SatellitePhonePowered)
        {
            return;
        }
        if (!cockpitState.SatellitePhoneConnected)
        {
            return;
        }
        if (!cockpitState.SatellitePhoneConnected)
        {
            return;
        }

        cockpitState.CommunicationStatus =
            "Emergency message transmitted by satellite.";

        await HandlePilotActionAsync(
            "Declare Emergency");
    }
    private async Task SetLandingGearPositionAsync(
        LandingGearState unit,
        bool down)
    {
        unit.Status =
            LandingGearStatusValue.Moving;

        await InvokeAsync(
            StateHasChanged);

        await Task.Delay(
            1500);

        if (selectedScenarioRecord.EmergencyType ==
            "Landing Gear Malfunction" &&
            down)
        {
            unit.Status =
                LandingGearStatusValue.Unsafe;

            return;
        }

        unit.Status =
            down
                ? LandingGearStatusValue.DownAndLocked
                : LandingGearStatusValue.UpAndLocked;
    }
    private async Task SetAllLandingGearAsync(
        bool down)
    {
        foreach (var unit in cockpitState.LandingGears)
        {
            await SetLandingGearPositionAsync(
                unit,
                down);
        }
    }
    private void HandleSatelliteConnection()
    {
        SetSatelliteConnection(
            !cockpitState.SatellitePhoneConnected);
    }
    private void HandleRadioPower()
    {
        SetRadioPower(
            !cockpitState.RadioPowered);
    }
    private void HandleSatellitePower()
    {
        SetSatellitePower(
            !cockpitState.SatellitePhonePowered);
    }
    private void HandleThrottleChanged( // Temp Wrapper until Razor Update
        EngineState engine,
        double power)
    {
        SetEnginePower(
            engine,
            power);
    }

    private void HandleFuelControlChanged(
        EngineState engine)
    {
        SetFuelCutoff(
            engine,
            !engine.FuelCutoff);
    }
    private async Task<bool>
        ActivateEngineFireSuppressionAsync(
            int engineNumber)
    {
        var engine =
            cockpitState.Engines
                .FirstOrDefault(
                    engine =>
                        engine.Number ==
                        engineNumber);

        if (engine is null)
        {
            return false;
        }

        await ActivateEngineFireSuppressionAsync(
            engine);

        return true;
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

    private async Task HandleFuelTankFocusedAsync(
        FuelState tank)
    {
        if (!emergencyTriggered)
        {
            return;
        }

        var nextStep =
            GetNextIncompleteProcedureStep();

        if (nextStep is null)
        {
            return;
        }

        // Step 1: simply inspecting either fuel gauge
        // counts as monitoring fuel quantity.
        if (string.Equals(
                nextStep.CorrectAction,
                "Monitor Fuel",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                "Monitor Fuel");

            return;
        }

        // Step 2: pilot must inspect the actual leaking tank.
        if (tank.Number ==
                cockpitState.LeakingFuelTankNumber &&
            string.Equals(
                nextStep.CorrectAction,
                $"Identify Fuel Leak Tank {tank.Number}",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                $"Identify Fuel Leak Tank {tank.Number}");
        }
    }
    private async Task HandleFuelTankIsolationAsync(
        FuelState tank)
    {
        if (!emergencyTriggered)
        {
            return;
        }

        if (tank.Number != cockpitState.LeakingFuelTankNumber)
        {
            _latestInstructorFeedback =
                new AiInstructorFeedback
                {
                    Severity = "Warning",
                    Message =
                        $"Tank {tank.Number} is not the leaking fuel source."
                };

            return;
        }

        var nextStep =
            GetNextIncompleteProcedureStep();

        // If the pilot immediately isolates the correct tank,
        // that demonstrates they monitored the fuel system.
        if (string.Equals(
                nextStep?.CorrectAction,
                "Monitor Fuel",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                "Monitor Fuel");

            nextStep =
                GetNextIncompleteProcedureStep();
        }

        // Correctly choosing the leaking tank also proves
        // that the pilot identified the leaking source.
        if (string.Equals(
                nextStep?.CorrectAction,
                $"Identify Fuel Leak Tank {tank.Number}",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                $"Identify Fuel Leak Tank {tank.Number}");

            nextStep =
                GetNextIncompleteProcedureStep();
        }

        // Finally record the actual isolation.
        if (string.Equals(
                nextStep?.CorrectAction,
                $"Isolate Fuel Tank {tank.Number}",
                StringComparison.OrdinalIgnoreCase))
        {
            await HandlePilotActionAsync(
                $"Isolate Fuel Tank {tank.Number}");
        }
    }
    private async Task ActivateEngineFireSuppressionAsync(
        EngineState engine)
    {
        engine.FireSuppressionActivated =
            true;

        await InvokeAsync(
            StateHasChanged);

        await Task.Delay(
            1500);

        if (selectedScenarioRecord.EmergencyType ==
            "Engine Fire")
        {
            var fireStillActive =
                engine.EngineFire ||
                engine.OnFire;

            engine.EngineFire =
                fireStillActive;

            engine.OnFire =
                fireStillActive;
        }

        await InvokeAsync(
            StateHasChanged);
    }
    private void ActivateBackupHydraulicSystem()
    {
        cockpitState.HydraulicPumpOnline = true;
        cockpitState.HydraulicPressure = 2200;
        cockpitState.HydraulicFault = false;

        cockpitState.AlertMessage =
            "BACKUP HYDRAULIC SYSTEM ACTIVE - PRESSURE RESTORED";
    }
    private double GetTargetAirspeed()
    {
        if (cockpitState.Engines.Count == 0)
        {
            return 0;
        }

        var averagePower =
            cockpitState.Engines
                .Average(engine => engine.Power);

        if (averagePower <= 0)
        {
            return 0;
        }

        /*
        * Our aircraft definitions use roughly 75%
        * engine power as normal cruise power.*
        * Therefore:
        *
        * 75% throttle -> aircraft cruise speed
        * 100% throttle -> above cruise speed
        */
        var powerRatio =
            averagePower / 75.0;

        var maximumTarget =
            selectedAircraft.CruiseSpeed * 1.20;

        return Math.Clamp(
            selectedAircraft.CruiseSpeed * powerRatio,
            0,
            maximumTarget);
    }

    /* ====================================================================================================
    |                                      AI Voice Commands                                              |
    ==================================================================================================== */

    private static string NormalizeVoiceCommand(
        string transcript)
    {
        var normalized =
            transcript
                .Trim()
                .ToLowerInvariant();

        var replacements =
            new Dictionary<string, string>
            {
                // Common numeric values
                ["one hundred"] = "100",
                ["ninety five"] = "95",
                ["ninety"] = "90",
                ["eighty five"] = "85",
                ["eighty"] = "80",
                ["seventy five"] = "75",
                ["seventy"] = "70",
                ["sixty five"] = "65",
                ["sixty"] = "60",
                ["fifty five"] = "55",
                ["fifty"] = "50",
                ["forty five"] = "45",
                ["forty"] = "40",
                ["thirty five"] = "35",
                ["thirty"] = "30",
                ["twenty five"] = "25",
                ["twenty"] = "20",
                ["fifteen"] = "15",
                ["ten"] = "10",

                // Engine numbers
                ["engine one"] = "engine 1",
                ["engine two"] = "engine 2",
                ["engine three"] = "engine 3",
                ["engine four"] = "engine 4",

                // Remaining small values
                ["five"] = "5",
                ["four"] = "4",
                ["three"] = "3",
                ["two"] = "2",
                ["one"] = "1",

                // Unit normalization
                ["degrees"] = "degree",
                ["percentage"] = "percent"
            };

        foreach (var replacement in replacements)
        {
            normalized =
                normalized.Replace(
                    replacement.Key,
                    replacement.Value,
                    StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }


    private static bool ContainsAny(
        string text,
        params string[] values)
    {
        return values.Any(
            value =>
                text.Contains(
                    value,
                    StringComparison.OrdinalIgnoreCase));
    }


    private static int? ExtractEngineNumber(
        string command)
    {
        var match =
            System.Text.RegularExpressions.Regex.Match(
                command,
                @"\bengine\s+(\d+)\b",
                System.Text.RegularExpressions.RegexOptions
                    .IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(
            match.Groups[1].Value,
            out var engineNumber)
                ? engineNumber
                : null;
    }


    private static double? ExtractCommandValue(
        string command)
    {
        /*
        * Use the LAST number in the command.
        *
        * Example:
        * "engine 1 power 100"
        *
        * Engine number = 1
        * Command value = 100
        */
        var matches =
            System.Text.RegularExpressions.Regex.Matches(
                command,
                @"-?\d+(?:\.\d+)?");

        if (matches.Count == 0)
        {
            return null;
        }

        var valueText =
            matches[^1].Value;

        return double.TryParse(
            valueText,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
                ? value
                : null;
    }


    private static double GetSignedEnginePowerChange(
        string command,
        double amount)
    {
        amount =
            Math.Abs(
                amount);

        if (ContainsAny(
                command,
                "decrease",
                "reduce",
                "lower",
                "drop"))
        {
            return -amount;
        }

        return amount;
    }


    private async Task<CockpitCommandResult>
        ExecuteVoiceCockpitCommandAsync(
            string transcript)
    {
        var command =
            NormalizeVoiceCommand(
                transcript);

        var engineNumber =
            ExtractEngineNumber(
                command);

        var value =
            ExtractCommandValue(
                command);
        // =========================================================
        // ENGINE READING FOCUS
        // =========================================================

        if (command.Contains("focus") &&
            command.Contains("engine"))
        {
            if (!engineNumber.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Specify which engine to focus.");
            }

            var engine =
                cockpitState.Engines.FirstOrDefault(
                    engine =>
                        engine.Number ==
                        engineNumber.Value);

            if (engine is null)
            {
                return CockpitCommandResult.Failure(
                    $"Engine {engineNumber.Value} does not exist " +
                    "on this aircraft.");
            }

            await HandleEngineStatusFocusAsync(
                engine);

            return CockpitCommandResult.Success(
                "Focus Engine Reading",
                $"Engine {engine.Number} selected for inspection.",
                $"engine.{engine.Number}");
        }

        // =========================================================
        // ENGINE POWER / THROTTLE
        // =========================================================

        var isThrottleCommand =
            command.Contains("throttle");

        var isEngineCommand =
            command.Contains("engine") &&
            !command.Contains("fuel") &&
            !command.Contains("fire");

        if (isThrottleCommand ||
            isEngineCommand)
        {
            Logger.LogInformation(
                "VOICE ENGINE COMMAND: " +
                "Transcript='{Transcript}', " +
                "Normalized='{Command}', " +
                "Engine={EngineNumber}, " +
                "Value={Value}",
                transcript,
                command,
                engineNumber,
                value);

            if (!value.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Engine power command requires a numeric value.");
            }

            var requestedPower =
                Math.Clamp(
                    Math.Abs(value.Value),
                    0,
                    100);


            // ---------------------------------------------------------
            // ALL ENGINES
            // ---------------------------------------------------------

            if (ContainsAny(
                    command,
                    "all engines",
                    "all engine",
                    "all throttles"))
            {
                /*
                * Relative:
                *
                * "Increase all engines by 10 percent"
                * "Reduce all throttles by 20 percent"
                */
                if (command.Contains("by"))
                {
                    var delta =
                        GetSignedEnginePowerChange(
                            command,
                            requestedPower);

                    foreach (var engine in cockpitState.Engines)
                    {
                        SetEnginePower(
                            engine,
                            engine.Power + delta);
                    }

                    return CockpitCommandResult.Success(
                        "Change All Engine Power",
                        $"All engine power changed by " +
                        $"{requestedPower:0} percent.",
                        "engines.power");
                }


                /*
                * Absolute:
                *
                * "Set all engines to 100 percent"
                * "Increase all engines to 100 percent"
                * "All throttles 100 percent"
                */
                SetAllEnginePower(
                    requestedPower);

                return CockpitCommandResult.Success(
                    "Set All Engine Power",
                    $"All engines set to " +
                    $"{requestedPower:0} percent.",
                    "engines.power");
            }


            // ---------------------------------------------------------
            // INDIVIDUAL ENGINE
            // ---------------------------------------------------------

            if (!engineNumber.HasValue)
            {
                /*
                * If this is a single-engine aircraft,
                * "set throttle to 100" is unambiguous.
                */
                if (cockpitState.Engines.Count == 1)
                {
                    var onlyEngine =
                        cockpitState.Engines[0];

                    if (command.Contains("by"))
                    {
                        var delta =
                            GetSignedEnginePowerChange(
                                command,
                                requestedPower);

                        SetEnginePower(
                            onlyEngine,
                            onlyEngine.Power + delta);
                    }
                    else
                    {
                        SetEnginePower(
                            onlyEngine,
                            requestedPower);
                    }

                    return CockpitCommandResult.Success(
                        "Set Engine Power",
                        $"Engine {onlyEngine.Number} power set to " +
                        $"{onlyEngine.Power:0} percent.",
                        $"engine.{onlyEngine.Number}.power");
                }

                return CockpitCommandResult.Failure(
                    "Specify which engine to control.");
            }


            var selectedEngine =
                cockpitState.Engines
                    .FirstOrDefault(
                        engine =>
                            engine.Number ==
                            engineNumber.Value);

            if (selectedEngine is null)
            {
                return CockpitCommandResult.Failure(
                    $"Engine {engineNumber.Value} does not exist " +
                    $"on this aircraft.");
            }


            /*
            * Relative:
            *
            * "Increase engine 1 power by 10 percent"
            * "Reduce engine 2 power by 20 percent"
            */
            if (command.Contains("by"))
            {
                var delta =
                    GetSignedEnginePowerChange(
                        command,
                        requestedPower);

                SetEnginePower(
                    selectedEngine,
                    selectedEngine.Power + delta);

                return CockpitCommandResult.Success(
                    "Change Engine Power",
                    $"Engine {selectedEngine.Number} power set to " +
                    $"{selectedEngine.Power:0} percent.",
                    $"engine.{selectedEngine.Number}.power");
            }


            /*
            * Absolute:
            *
            * "Set engine 1 power to 100 percent"
            * "Increase engine 1 power to 100 percent"
            * "Engine 1 power 100"
            * "Throttle engine 1 to 100"
            */
            SetEnginePower(
                selectedEngine,
                requestedPower);

            return CockpitCommandResult.Success(
                "Set Engine Power",
                $"Engine {selectedEngine.Number} power set to " +
                $"{selectedEngine.Power:0} percent.",
                $"engine.{selectedEngine.Number}.power");
        }


        // =========================================================
        // PITCH
        // =========================================================

        if (command.Contains("pitch"))
        {
            if (!value.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Pitch command requires a degree value.");
            }

            var degrees =
                Math.Abs(
                    value.Value);

            if (ContainsAny(
                    command,
                    "increase",
                    "raise",
                    "pitch up"))
            {
                if (command.Contains("to"))
                {
                    SetPitch(
                        degrees);

                    return CockpitCommandResult.Success(
                        "Set Pitch",
                        $"Pitch set to {cockpitState.Pitch:0} degrees.",
                        "flight.attitude");
                }

                ChangePitch(
                    degrees);

                return CockpitCommandResult.Success(
                    "Increase Pitch",
                    $"Pitch increased to " +
                    $"{cockpitState.Pitch:0} degrees.",
                    "flight.attitude");
            }

            if (ContainsAny(
                    command,
                    "decrease",
                    "lower",
                    "pitch down"))
            {
                if (command.Contains("to"))
                {
                    SetPitch(
                        -degrees);

                    return CockpitCommandResult.Success(
                        "Set Pitch",
                        $"Pitch set to {cockpitState.Pitch:0} degrees.",
                        "flight.attitude");
                }

                ChangePitch(
                    -degrees);

                return CockpitCommandResult.Success(
                    "Decrease Pitch",
                    $"Pitch decreased to " +
                    $"{cockpitState.Pitch:0} degrees.",
                    "flight.attitude");
            }

            SetPitch(
                value.Value);

            return CockpitCommandResult.Success(
                "Set Pitch",
                $"Pitch set to {cockpitState.Pitch:0} degrees.",
                "flight.attitude");
        }


        // =========================================================
        // BANK
        // =========================================================

        if (command.Contains("bank"))
        {
            if (!value.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Bank command requires a degree value.");
            }

            var degrees =
                Math.Abs(
                    value.Value);

            if (command.Contains("left"))
            {
                if (command.Contains("by"))
                {
                    ChangeBank(
                        -degrees);
                }
                else
                {
                    SetBank(
                        -degrees);
                }

                return CockpitCommandResult.Success(
                    "Bank Left",
                    $"Bank set to " +
                    $"{Math.Abs(cockpitState.Bank):0} degrees left.",
                    "flight.attitude");
            }

            if (command.Contains("right"))
            {
                if (command.Contains("by"))
                {
                    ChangeBank(
                        degrees);
                }
                else
                {
                    SetBank(
                        degrees);
                }

                return CockpitCommandResult.Success(
                    "Bank Right",
                    $"Bank set to " +
                    $"{Math.Abs(cockpitState.Bank):0} degrees right.",
                    "flight.attitude");
            }

            SetBank(
                value.Value);

            return CockpitCommandResult.Success(
                "Set Bank",
                $"Bank set to {cockpitState.Bank:0} degrees.",
                "flight.attitude");
        }
        // =========================================================
        // BACKUP HYDRAULIC SYSTEM
        // =========================================================

        if (ContainsAny(
                command,
                "activate backup hydraulic system",
                "activate backup hydraulics",
                "enable backup hydraulic system",
                "enable backup hydraulics",
                "turn on backup hydraulic system",
                "turn on backup hydraulics",
                "backup hydraulic system on",
                "backup hydraulics on"))
        {
            ActivateBackupHydraulicSystem();

            return CockpitCommandResult.Success(
                "Activate Backup Hydraulic System",
                "Backup hydraulic system activated.",
                "hydraulic.backup");
        }

        // =========================================================
        // RUDDER
        // =========================================================

        if (command.Contains("rudder"))
        {
            if (ContainsAny(
                    command,
                    "center",
                    "centre",
                    "neutral"))
            {
                SetRudderPosition(
                    0);

                return CockpitCommandResult.Success(
                    "Center Rudder",
                    "Rudder centered.",
                    "flight.rudder");
            }

            if (!value.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Rudder command requires a percentage value.");
            }

            var normalizedPosition =
                Math.Clamp(
                    Math.Abs(value.Value) / 100.0,
                    0,
                    1);

            if (command.Contains("left"))
            {
                SetRudderPosition(
                    -normalizedPosition);

                return CockpitCommandResult.Success(
                    "Rudder Left",
                    $"Rudder set to " +
                    $"{Math.Abs(value.Value):0} percent left.",
                    "flight.rudder");
            }

            if (command.Contains("right"))
            {
                SetRudderPosition(
                    normalizedPosition);

                return CockpitCommandResult.Success(
                    "Rudder Right",
                    $"Rudder set to " +
                    $"{Math.Abs(value.Value):0} percent right.",
                    "flight.rudder");
            }

            return CockpitCommandResult.Failure(
                "Specify rudder left, right, or center.");
        }


        // =========================================================
        // FUEL CUTOFF
        // =========================================================

        if (command.Contains("fuel") &&
            command.Contains("engine"))
        {
            if (!engineNumber.HasValue)
            {
                return CockpitCommandResult.Failure(
                    "Specify an engine number for fuel control.");
            }

            if (ContainsAny(
                    command,
                    "cut off",
                    "cutoff",
                    "shut off",
                    "fuel off"))
            {
                if (!SetFuelCutoff(
                        engineNumber.Value,
                        true))
                {
                    return CockpitCommandResult.Failure(
                        $"Engine {engineNumber.Value} does not exist.");
                }

                return CockpitCommandResult.Success(
                    "Fuel Cutoff",
                    $"Fuel cut off to engine " +
                    $"{engineNumber.Value}.",
                    $"engine.{engineNumber.Value}.fuel");
            }

            if (ContainsAny(
                    command,
                    "restore",
                    "fuel on",
                    "open fuel",
                    "restore fuel"))
            {
                if (!SetFuelCutoff(
                        engineNumber.Value,
                        false))
                {
                    return CockpitCommandResult.Failure(
                        $"Engine {engineNumber.Value} does not exist.");
                }

                return CockpitCommandResult.Success(
                    "Restore Fuel",
                    $"Fuel restored to engine " +
                    $"{engineNumber.Value}.",
                    $"engine.{engineNumber.Value}.fuel");
            }
        }


        // =========================================================
        // LANDING GEAR
        // =========================================================

        if (ContainsAny(
                command,
                "landing gear",
                "gear"))
        {
            if (ContainsAny(
                    command,
                    "down",
                    "lower",
                    "extend"))
            {
                await SetAllLandingGearAsync(
                    true);

                return CockpitCommandResult.Success(
                    "Gear Down",
                    "Landing gear lowered.",
                    "flight.landing-gear");
            }

            if (ContainsAny(
                    command,
                    "up",
                    "raise",
                    "retract"))
            {
                await SetAllLandingGearAsync(
                    false);

                return CockpitCommandResult.Success(
                    "Gear Up",
                    "Landing gear retracted.",
                    "flight.landing-gear");
            }
        }


        // =========================================================
        // RADIO
        // =========================================================

        if (command.Contains("radio"))
        {
            if (ContainsAny(
                    command,
                    "turn on",
                    "power on",
                    "radio on"))
            {
                SetRadioPower(
                    true);

                return CockpitCommandResult.Success(
                    "Radio Power On",
                    "Radio powered on.",
                    "communication.radio");
            }

            if (ContainsAny(
                    command,
                    "turn off",
                    "power off",
                    "radio off"))
            {
                SetRadioPower(
                    false);

                return CockpitCommandResult.Success(
                    "Radio Power Off",
                    "Radio powered off.",
                    "communication.radio");
            }

            if (ContainsAny(
                    command,
                    "guard frequency",
                    "guard",
                    "121.5"))
            {
                if (!cockpitState.RadioPowered)
                {
                    return CockpitCommandResult.Failure(
                        "Radio must be powered on before selecting guard.");
                }

                HandleGuardFrequency();

                return CockpitCommandResult.Success(
                    "Select Guard Frequency",
                    "Radio set to guard frequency 121.5.",
                    "communication.radio.frequency");
            }
        }


        // =========================================================
        // SATELLITE PHONE
        // =========================================================

        if (ContainsAny(
                command,
                "satellite",
                "sat phone"))
        {
            if (ContainsAny(
                    command,
                    "power on",
                    "turn on"))
            {
                SetSatellitePower(
                    true);

                return CockpitCommandResult.Success(
                    "Satellite Power On",
                    "Satellite phone powered on.",
                    "communication.satellite");
            }

            if (ContainsAny(
                    command,
                    "power off",
                    "turn off"))
            {
                SetSatellitePower(
                    false);

                return CockpitCommandResult.Success(
                    "Satellite Power Off",
                    "Satellite phone powered off.",
                    "communication.satellite");
            }

            /*
            * Check disconnect before connect because
            * "disconnect" contains "connect".
            */
            if (command.Contains("disconnect"))
            {
                SetSatelliteConnection(
                    false);

                return CockpitCommandResult.Success(
                    "Disconnect Satellite",
                    "Satellite connection disconnected.",
                    "communication.satellite.connection");
            }

            if (command.Contains("connect"))
            {
                if (!cockpitState.SatellitePhonePowered)
                {
                    return CockpitCommandResult.Failure(
                        "Satellite phone must be powered on first.");
                }

                SetSatelliteConnection(
                    true);

                return CockpitCommandResult.Success(
                    "Connect Satellite",
                    "Satellite connection established.",
                    "communication.satellite.connection");
            }

            if (command.Contains("emergency"))
            {
                if (!cockpitState.SatellitePhonePowered)
                {
                    return CockpitCommandResult.Failure(
                        "Satellite phone must be powered on first.");
                }

                if (!cockpitState.SatellitePhoneConnected)
                {
                    return CockpitCommandResult.Failure(
                        "Satellite phone must be connected first.");
                }

                /*
                * Keep using your existing communication behavior.
                */
                await HandleSatelliteEmergency();

                return CockpitCommandResult.Success(
                    "Satellite Emergency",
                    "Satellite emergency message transmitted.",
                    "communication.satellite.emergency");
            }
        }

        // =========================================================
        // BACKUP ELECTRICAL POWER
        // =========================================================

        if (ContainsAny(
                command,
                "activate backup power",
                "backup power",
                "emergency power",
                "switch to backup power"))
        {
            await HandlePilotActionAsync(
                "Activate Backup Power");

            return CockpitCommandResult.Success(
                "Activate Backup Power",
                "Backup electrical power activated.",
                "electrical.backup-power");
        }


        // =========================================================
        // SHED NON-ESSENTIAL ELECTRICAL LOAD
        // =========================================================

        if (ContainsAny(
                command,
                "reduce electrical load",
                "shed electrical load",
                "shed non-essential load",
                "shed non essential load",
                "reduce power load",
                "disconnect non-essential systems",
                "disconnect non essential systems"))
        {
            await HandlePilotActionAsync(
                "Reduce Electrical Load");

            return CockpitCommandResult.Success(
                "Reduce Electrical Load",
                "Non-essential electrical load shed.",
                "electrical.load");
        }


        // =========================================================
        // ENGINE FIRE SUPPRESSION
        // =========================================================

        if (command.Contains("fire") &&
            ContainsAny(
                command,
                "suppress",
                "suppression",
                "extinguish"))
        {
            /*
            * If an engine number was spoken, target that
            * specific engine.
            *
            * "Activate fire suppression engine 2"
            */
            if (engineNumber.HasValue)
            {
                var succeeded =
                    await ActivateEngineFireSuppressionAsync(
                        engineNumber.Value);

                if (!succeeded)
                {
                    return CockpitCommandResult.Failure(
                        $"Engine {engineNumber.Value} does not exist.");
                }

                return CockpitCommandResult.Success(
                    "Activate Engine Fire Suppression",
                    $"Fire suppression activated for " +
                    $"engine {engineNumber.Value}.",
                    $"engine.{engineNumber.Value}.fire-suppression");
            }

            /*
            * No engine was specified:
            *
            * "Activate engine fire suppression"
            *
            * Behave exactly like the physical button,
            * which uses GetAffectedEngine().
            */
            await ActivateFireSuppression();

            var affectedEngine =
                GetAffectedEngine();

            return CockpitCommandResult.Success(
                "Activate Engine Fire Suppression",
                affectedEngine is null
                    ? "Engine fire suppression activated."
                    : $"Fire suppression activated for " +
                    $"engine {affectedEngine.Number}.",
                affectedEngine is null
                    ? "engine.fire-suppression"
                    : $"engine.{affectedEngine.Number}.fire-suppression");
        }
        
        // =========================================================
        // OXYGEN MASKS & SENDING RADIO CODES
        // =========================================================
        if (ContainsAny(
                command,
                "oxygen mask",
                "oxygen masks",
                "put on oxygen mask",
                "put on oxygen masks",
                "don oxygen mask",
                "don oxygen masks",
                "masks on"))
        {
            await HandlePilotActionAsync(
                "Oxygen Masks");

            return CockpitCommandResult.Success(
                "Oxygen Masks",
                "Oxygen masks deployed.",
                "cabin.oxygen");
        }
        if (ContainsAny(
                command,
                "transmit code",
                "transmit emergency code",
                "set emergency code",
                "send emergency code",
                "squawk 7700",
                "transponder 7700",
                "set transponder 7700"))
        {
            if (!cockpitState.RadioPowered)
            {
                return CockpitCommandResult.Failure(
                    "Radio must be powered on before transmitting the emergency code.");
            }

            await HandlePilotActionAsync(
                "Set Emergency Code");

            return CockpitCommandResult.Success(
                "Set Emergency Code",
                "Emergency code 7700 transmitted.",
                "communication.transponder");
        }
        // =========================================================
        // UNKNOWN COMMAND
        // =========================================================

        return CockpitCommandResult.Failure(
            $"Cockpit command '{transcript}' was not recognized.");
    }
    
    /* ===================================================================================================
     |                                     Focus Commands and Helpers                                      |
     ==================================================================================================== */
    private async Task<bool> FocusCockpitInstrumentAsync(
       InstrumentDefinition instrument)
    {
        if (string.IsNullOrWhiteSpace(
                instrument.ControlId))
        {
            return false;
        }

        return await JSRuntime.InvokeAsync<bool>(
            "aeroFocus.focusControl",
            instrument.ControlId);
    }
    private async Task<bool> FocusCockpitControlAsync(
            string controlId)
    {
        if (string.IsNullOrWhiteSpace(
                controlId))
        {
            return false;
        }

        return await JSRuntime.InvokeAsync<bool>(
            "aeroFocus.focusControl",
            controlId);
    }
    private InstrumentDefinition? FindInstrumentForVoiceFocus(
           string command)
    {
        var normalized =
            NormalizeVoiceCommand(
                command);

        /*
        * Strip the focus language so we're left
        * primarily with the instrument name.*/
        var target =
            normalized
                .Replace(
                    "set focus to",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "set focus on",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "focus on",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "focus",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Trim();

        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        foreach (var instrument in
                cockpitLayout.Instruments)
        {
            /*
            * Display name:
            *
            * "Vertical Speed"
            * "Altitude"
            * "Aircraft Attitude"
            */
            if (!string.IsNullOrWhiteSpace(
                    instrument.DisplayName) &&
                target.Contains(
                    instrument.DisplayName.ToLowerInvariant(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return instrument;
            }

            /*
            * Control ID:
            *
            * flight.airspeed
            * flight.altitude
            * flight.heading
            */
            if (!string.IsNullOrWhiteSpace(
                    instrument.ControlId))
            {
                var controlName =
                    instrument.ControlId
                        .Split('.')
                        .Last()
                        .Replace("-", " ");

                if (target.Contains(
                        controlName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return instrument;
                }
            }

            /*
            * Voice aliases defined by the
            * aircraft cockpit layout.*/
            foreach (var alias in
                    instrument.VoiceAliases)
            {
                if (target.Contains(
                        alias,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return instrument;
                }
            }
        }

        return null;
    }


    /* ====================================================================================================
     |                                          Debug Controls                                              |
     ===================================================================================================== */
    private void DebugSetCruiseAltitude()
    {
        var defaults =
            cockpitLayout.DefaultState;

        cockpitState.Altitude =
            10500;

        cockpitState.Airspeed =
            defaults.CruiseAirspeed;

        cockpitState.VerticalSpeed = 0;
        cockpitState.DisplayedVerticalSpeed = 0;
        cockpitState.Pitch = 0;
        cockpitState.Bank = 0;
        cockpitState.FlightPhase = "Cruise";

        foreach (var engine in cockpitState.Engines)
        {
            engine.Running = true;
            engine.Power =
                defaults.NormalEnginePower;
        }
    }
}