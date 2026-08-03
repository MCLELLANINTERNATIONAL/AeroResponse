using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;
using Microsoft.AspNetCore.Components;
using SimulationSelectionModel = AeroResponse.Models.SimulationSelection;
using VSIMath = AeroResponse.Simulation.Instruments.VerticalSpeedIndicator.VSIMath;
namespace AeroResponse.Components.Pages;

public partial class Simulation : IAsyncDisposable
{
    private CockpitState cockpitState = new();

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
        if (!_needsStorageCheck)
        {
            return;
        }

        _needsStorageCheck = false;

        var savedSelection = await SelectionStorage.GetAsync();

        string aircraftKey;
        string scenarioType;

        if (savedSelection?.IsValid == true &&
            int.TryParse(savedSelection.AircraftKey, out _)) // Making sure we have a valid Aircraft Key
        {
            aircraftKey = savedSelection.AircraftKey;
            scenarioType = savedSelection.ScenarioType;
        }
        else
        {
            // Fallback to first Aircraft and Scenario if none is present in load

            var defaultAircraft = _aircraftOptions.FirstOrDefault();
            var defaultScenario = _scenarioRecords.FirstOrDefault();

            if (defaultAircraft is null || defaultScenario is null)
            {
                _loadFailed = true;
                await InvokeAsync(StateHasChanged);
                return;
            }

            aircraftKey = defaultAircraft.Id.ToString();
            scenarioType = defaultScenario.EmergencyType;
        }

        await LoadSelectionAsync(aircraftKey, scenarioType, true);

        await InvokeAsync(StateHasChanged);
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
                await AircraftService.GetByIdAsync(aircraftId);

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

    private void ToggleAircraftMenu()
    {
        _isAircraftMenuOpen =
            !_isAircraftMenuOpen;

        _isScenarioMenuOpen = false;
    }
    private void ActivateFireSuppression()
    {
        cockpitState.FireSuppressionActivated = true;
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

        await InitializeSimulationStateAsync();
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

    private void UpdateSimulationUrl()
    {
        var targetUrl = $"/simulation/{Uri.EscapeDataString(selectedAircraft.Id.ToString())}/{Uri.EscapeDataString(selectedScenarioRecord.EmergencyType)}";

        if (string.Equals(Navigation.Uri, Navigation.ToAbsoluteUri(targetUrl).ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Navigation.NavigateTo(targetUrl, replace: true);
    }

    private async Task InitializeSimulationStateAsync()
    {
        simulationStartedAt = DateTime.UtcNow;
        manualTriggerRequested = false;

        emergencyTriggered =
            string.Equals(
                selectedScenarioRecord.TriggerType,
                "Immediate",
                StringComparison.OrdinalIgnoreCase);

        cockpitState = emergencyTriggered
            ? selectedRuntimeScenario.Start(cockpitLayout)
            : CreateNormalCockpitState();

        // Runtime scenario definitions are the source of truth for the
        // executable cockpit actions and their required sequence.
        procedureSteps = SimulationEngine.GetProcedureSteps(
            selectedScenarioRecord.EmergencyType,
            cockpitLayout,
            selectedScenarioRecord.Id);

        // Apply scenario-wide timing defaults to executable steps.
        foreach (var step in procedureSteps)
        {
            step.MaxResponseSeconds = Math.Min(
                selectedScenarioRecord.TimeLimitSeconds,
                Math.Max(
                    step.MaxResponseSeconds,
                    10 + ((step.StepOrder - 1) * 15)));
        }

        SimulationSession.StartSimulation(
            userId: "test-pilot",
            aircraftId: selectedAircraft.Id,
            scenario: selectedScenarioRecord,
            aircraft: cockpitLayout,
            expectedSteps: procedureSteps,
            pilotName: "Pilot");

        if (emergencyTriggered &&
            !SimulationSession.EmergencyTriggeredAt.HasValue)
        {
            SimulationSession.MarkEmergencyTriggered(simulationStartedAt);
        }

        _completedReport = null;
        _isCompleting = false;
        _remainingSeconds = selectedScenarioRecord.TimeLimitSeconds;
        _completedProcedureStepOrders.Clear();

        cockpitState.DisplayedVerticalSpeed =
            cockpitState.VerticalSpeed;

        _isReady = true;
        _loadFailed = false;

        CloseSelectorMenus();

        if (_simulationLoop is null)
        {
            StartSimulationLoop();
        }
    }

    private bool IsProcedureStepCompleted(
        ScenarioProcedureStep step)
    {
        return _completedProcedureStepOrders.Contains(
            step.StepOrder);
    }

    private void ToggleProcedureStep(
        ScenarioProcedureStep step,
        ChangeEventArgs eventArgs)
    {
        if (!emergencyTriggered)
        {
            return;
        }

        var isChecked =
            eventArgs.Value is bool boolValue
                ? boolValue
                : bool.TryParse(
                    eventArgs.Value?.ToString(),
                    out var parsedValue) &&
                parsedValue;

        if (isChecked)
        {
            _completedProcedureStepOrders.Add(
                step.StepOrder);

            if (!string.IsNullOrWhiteSpace(
                step.CorrectAction) &&
                _completedReport is null)
            {
                cockpitState = SimulationSession.SubmitPilotAction(
                    step.CorrectAction,
                    step.StepOrder);
            }
        }
        else
        {
            _completedProcedureStepOrders.Remove(
                step.StepOrder);
        }
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
                Power = defaults.NormalEnginePower,
                Running = true,
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
            Airspeed = defaults.CruiseAirspeed,
            Altitude = defaults.CruiseAltitude,
            Heading = defaults.DefaultHeading,
            VerticalSpeed = defaults.DefaultVerticalSpeed,
            DisplayedVerticalSpeed = defaults.DefaultVerticalSpeed,
            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,
            FlightPhase = DetermineFlightPhase(
                defaults.CruiseAltitude,
                defaults.CruiseAirspeed,
                defaults.DefaultVerticalSpeed),

            Engines = engines,
            Brakes = brakes,
            FuelTanks = fuelTanks,
            FuelPercentage = defaults.FuelPercentage,
            AlertMessage = "Systems Normal",

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
        if (altitude <= 0 && airspeed < 40)
        {
            return "Ground";
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

        if (verticalSpeed < -300 &&
            altitude > 3_000)
        {
            return "Descent";
        }

        if (altitude <= 3_000 &&
            altitude > 500 &&
            verticalSpeed < 0)
        {
            return "Approach";
        }

        if (altitude <= 500 &&
            verticalSpeed <= 0 &&
            airspeed > 40)
        {
            return "Landing";
        }

        return "Cruise";
    }
    private void EvaluateEmergencyTrigger()
    {
        if (emergencyTriggered || !_isReady)
        {
            return;
        }

        var elapsed =
            DateTime.UtcNow - simulationStartedAt;

        var shouldTrigger =
            TriggerEvaluator.ShouldTrigger(
                selectedScenarioRecord,
                cockpitState,
                elapsed,
                manualTriggerRequested);

        if (!shouldTrigger)
        {
            return;
        }

        ActivateEmergencyScenario();
    }

    private void ActivateEmergencyScenario()
    {
        if (emergencyTriggered)
        {
            return;
        }

        emergencyTriggered = true;

        SimulationSession.MarkEmergencyTriggered();

        cockpitState =
            selectedRuntimeScenario.Start(
                cockpitLayout);

        cockpitState.DisplayedVerticalSpeed =
            cockpitState.VerticalSpeed;
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

                UpdateFlightPhase();

                EvaluateEmergencyTrigger();

                if (emergencyTriggered && _completedReport is null)
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

        _simulationTimer?.Dispose();
        _simulationCancellation?.Dispose();
    }
}