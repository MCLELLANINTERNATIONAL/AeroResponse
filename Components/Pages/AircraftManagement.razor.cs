using AeroResponse.Models;
using AeroResponse.Services;
using AeroResponse.Simulation.Layouts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using CockpitLayoutModel = AeroResponse.Models.CockpitLayout;


namespace AeroResponse.Components.Pages;

public partial class AircraftManagement : ComponentBase
{
    [Inject]
    private ILogger<AircraftManagement> Logger { get; set; } = default!;
    [Inject]
    private AircraftService AircraftService { get; set; } = default!;

    [Inject]
    private CockpitLayoutService CockpitLayoutService { get; set; } = default!;

    [Inject]
    private ICockpitLayoutProvider LayoutProvider { get; set; } = default!;

    private IReadOnlyList<Aircraft> aircraftList = [];

    private List<AvailableLayout> availableLayouts = [];

    private Aircraft selectedAircraft = CreateEmptyAircraft();

    private int selectedAircraftId;

    private bool isLoading = true;
    private bool isSaving;

    private string? statusMessage;
    private bool showLayoutOverlay;
    private bool isDeletingLayout;

    private bool isEditingLayout;

    private string? originalLayoutKey;

    private string? layoutEditorError;

    private CockpitLayoutEditor layoutEditor = CreateEmptyLayoutEditor();

    private bool showDeleteLayoutOverlay;

    private CockpitLayoutDefinition? layoutPendingDeletion;
    private CockpitLayoutDefinition? layoutBeingEdited;

    private List<AffectedAircraftEditor> affectedAircraft = [];

    private string? deleteLayoutError;

    private int? editingLayoutDatabaseId;

    private bool editingLayoutIsBuiltIn;

    private int? layoutPendingDeletionDatabaseId;

    private VsiAdvancedEditor? advancedVsi;

    /*=============================================================================
     |                            Helper Classes                                   |
     =============================================================================*/

    private sealed class VsiCalibrationPointEditor
    {
        public int VerticalSpeed { get; set; }

        public double Angle { get; set; }
    }

    private sealed class VsiAdvancedEditor
    {
        public int MinimumVerticalSpeed { get; set; }

        public int MaximumVerticalSpeed { get; set; }

        public int LagSeconds { get; set; }

        public List<VsiCalibrationPointEditor>
            CalibrationPoints
        { get; set; } = [];
    }
    private VerticalSpeedIndicatorLayout
        CreateVsiFromAdvancedEditor()
    {
        if (advancedVsi is null)
        {
            return CreateDefaultVsiLayout();
        }

        return new VerticalSpeedIndicatorLayout
        {
            MinimumVerticalSpeed =
                advancedVsi.MinimumVerticalSpeed,

            MaximumVerticalSpeed =
                advancedVsi.MaximumVerticalSpeed,

            LagSeconds =
                advancedVsi.LagSeconds,

            CalibrationPoints =
                advancedVsi.CalibrationPoints
                    .Select(point =>
                        new VSICalibrationPoint(
                            point.VerticalSpeed,
                            point.Angle))
                    .ToList()
        };
    }
    private static CockpitLayoutDefinition ConvertToLayoutDefinition(
        CockpitLayoutModel layout)
    {
        return new CockpitLayoutDefinition
        {
            Key = layout.Key,
            Name = layout.Name,

            AircraftId =
                layout.Details.AircraftId,

            Rows =
                layout.Details.Rows,

            Columns =
                layout.Details.Columns,

            EngineCount =
                layout.Details.EngineCount,

            Instruments =
                layout.Details.Instruments
                    .Select(CloneInstrument)
                    .ToList(),

            Airspeed =
                NormalizeAirspeedLayout(
                    layout.Details.Airspeed),

            ArtificialHorizon =
                layout.Details.ArtificialHorizon
                ?? CreateDefaultArtificialHorizonLayout(),

            VSI =
                layout.Details.VSI
                ?? CreateDefaultVsiLayout(),

            DefaultState =
                layout.Details.DefaultState
                ?? CreateDefaultAircraftState()
        };
    }
    private static string GetLandingGearKindName(
        LandingGearKind kind)
    {
        return kind switch
        {
            LandingGearKind.FixedTricycle =>
                "Fixed Tricycle",

            LandingGearKind.RetractableTricycle =>
                "Retractable Tricycle",

            LandingGearKind.Tailwheel =>
                "Tailwheel",

            LandingGearKind.MultiBogey =>
                "Multi-Bogey",

            LandingGearKind.Tandem =>
                "Tandem",

            LandingGearKind.Floats =>
                "Floats",

            LandingGearKind.Skis =>
                "Skis",

            _ => kind.ToString()
        };
    }
    private void RemoveLandingGearUnit(
        LandingGearUnit unit)
    {
        selectedAircraft
            .LandingGearConfig
            .Units
            .Remove(unit);

        var units =
            selectedAircraft
                .LandingGearConfig
                .Units
                .OrderBy(item => item.Order)
                .ToList();

        for (var index = 0;
            index < units.Count;
            index++)
        {
            units[index].Number = index + 1;
            units[index].Order = index;
        }
    }
    private void OnLandingGearKindChanged()
    {
        var selectedKind =
            selectedAircraft.LandingGearConfig.Kind;

        selectedAircraft.LandingGearConfig =
            CreateLandingGearConfig(selectedKind);
    }
    private static AirspeedIndicatorLayout NormalizeAirspeedLayout(
        AirspeedIndicatorLayout? airspeed)
    {
        if (airspeed is null ||
            airspeed.MaximumSpeed <= airspeed.MinimumSpeed)
        {
            return CreateDefaultAirspeedLayout();
        }

        return airspeed;
    }


    /*=============================================================================
     |                        Initialization and CRUD                              |
     =============================================================================*/

    protected override async Task OnInitializedAsync()
    {
        await LoadLayoutsAsync();
        await LoadAircraftAsync();

        isLoading = false;
    }

    private async Task LoadAircraftAsync()
    {
        aircraftList = await AircraftService.GetAllAsync();
        LoadAdvancedVsi();
    }
    private void LoadAdvancedVsi()
    {
        var layout =
            availableLayouts
                .FirstOrDefault(item =>
                    string.Equals(
                        item.Definition.Key,
                        selectedAircraft.CockpitLayoutKey,
                        StringComparison.OrdinalIgnoreCase));

        if (layout is null)
        {
            advancedVsi = null;
            return;
        }

        var vsi =
            layout.Definition.VSI;

        advancedVsi = new VsiAdvancedEditor
        {
            MinimumVerticalSpeed =
                vsi.MinimumVerticalSpeed,

            MaximumVerticalSpeed =
                vsi.MaximumVerticalSpeed,

            LagSeconds =
                vsi.LagSeconds,

            CalibrationPoints =
                vsi.CalibrationPoints
                    .Select(point =>
                        new VsiCalibrationPointEditor
                        {
                            VerticalSpeed =
                                point.VerticalSpeed,

                            Angle =
                                point.Angle
                        })
                    .ToList()
        };
    }

    private async Task LoadLayoutsAsync()
    {
        var builtInLayouts = await LayoutProvider.GetLayouts();
        var savedLayouts = await CockpitLayoutService.GetAllAsync();

        var layoutsByKey = new Dictionary<string, AvailableLayout>(StringComparer.OrdinalIgnoreCase);

        foreach (var builtIn in builtInLayouts)
        {
            layoutsByKey[builtIn.Key] = new AvailableLayout
            {
                DatabaseId = null,
                Definition = builtIn,
                IsBuiltIn = true
            };
        }

        foreach (var saved in savedLayouts)
        {
            layoutsByKey[saved.Key] = new AvailableLayout
            {
                DatabaseId = saved.Id,
                Definition = ConvertToLayoutDefinition(saved),
                IsBuiltIn = saved.IsBuiltIn
            };
        }

        availableLayouts = layoutsByKey.Values
            .OrderBy(layout => layout.Definition.Name)
            .ToList();
    }

    private async Task SelectAircraftAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var aircraftId))
        {
            return;
        }

        selectedAircraftId = aircraftId;
        statusMessage = null;

        if (aircraftId == 0)
        {
            StartNewAircraft();
            return;
        }

        var aircraft = await AircraftService.GetByIdWithLandingGearAsync(aircraftId);

        if (aircraft is null)
        {
            statusMessage = "Aircraft not found.";
            await LoadAircraftAsync();
            return;
        }

        selectedAircraft = aircraft;
        LoadAdvancedVsi();
    }

    private void StartNewAircraft()
    {
        selectedAircraftId = 0;
        selectedAircraft = CreateEmptyAircraft();
        statusMessage = null;
    }

    private void AddLandingGearUnit()
    {
        var nextNumber = selectedAircraft.LandingGearConfig.Units.Count + 1;

        selectedAircraft.LandingGearConfig.Units.Add(new LandingGearUnit
        {
            Number = nextNumber,
            Label = $"Gear {nextNumber}",
            Position = LandingGearPosition.Custom,
            Order = selectedAircraft.LandingGearConfig.Units.Count
        });
    }
    private static AircraftLandingGearConfig CreateDefaultLandingGear()
    {
        return CreateLandingGearConfig(
            LandingGearKind.FixedTricycle);
    }
    private static AircraftLandingGearConfig CreateLandingGearConfig(
        LandingGearKind kind)
    {
        var config = new AircraftLandingGearConfig
        {
            Kind = kind
        };

        switch (config.Kind)
        {
            case LandingGearKind.FixedTricycle:
            case LandingGearKind.RetractableTricycle:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "N", Position = LandingGearPosition.Nose, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "L", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 1 },
                    new LandingGearUnit { Number = 3, Label = "R", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 2 }
                });
                break;

            case LandingGearKind.Tailwheel:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "L", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "R", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 1 },
                    new LandingGearUnit { Number = 3, Label = "T", Position = LandingGearPosition.Tail, Status = LandingGearStatusValue.DownAndLocked, Order = 2 }
                });
                break;

            case LandingGearKind.Tandem:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "F", Position = LandingGearPosition.Nose, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "A", Position = LandingGearPosition.Tail, Status = LandingGearStatusValue.DownAndLocked, Order = 1 }
                });
                break;

            case LandingGearKind.MultiBogey:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "L1", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "L2", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 1 },
                    new LandingGearUnit { Number = 3, Label = "R1", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 2 },
                    new LandingGearUnit { Number = 4, Label = "R2", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 3 }
                });
                break;

            case LandingGearKind.Floats:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "L Float", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "R Float", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 1 }
                });
                break;

            case LandingGearKind.Skis:
                config.Units.AddRange(new[]
                {
                    new LandingGearUnit { Number = 1, Label = "L Ski", Position = LandingGearPosition.LeftMain, Status = LandingGearStatusValue.DownAndLocked, Order = 0 },
                    new LandingGearUnit { Number = 2, Label = "R Ski", Position = LandingGearPosition.RightMain, Status = LandingGearStatusValue.DownAndLocked, Order = 1 }
                });
                break;

            default:
                break;
        }

        return config;
    }

    private async Task SaveAircraftAsync(EditContext editContext)
    {
        if (string.IsNullOrWhiteSpace(selectedAircraft.CockpitLayoutKey))
        {
            statusMessage = "Select a cockpit layout before saving.";
            return;
        }

        if (!LayoutExists(selectedAircraft.CockpitLayoutKey))
        {
            statusMessage = "The selected cockpit layout is not installed.";
            return;
        }

        isSaving = true;
        statusMessage = null;

        try
        {
            if (selectedAircraft.Id == 0)
            {
                selectedAircraft =
                    await AircraftService.CreateAsync(
                        selectedAircraft);

                selectedAircraftId =
                    selectedAircraft.Id;

                statusMessage =
                    $"{selectedAircraft.Name} was created.";
            }
            else
            {
                await AircraftService.UpdateAsync(
                    selectedAircraft);

                statusMessage =
                    $"{selectedAircraft.Name} was updated.";
            }

            // Save the advanced configuration belonging
            // to the selected cockpit layout.
            await SaveAdvancedVsiAsync();

            await LoadLayoutsAsync();
            await LoadAircraftAsync();
        }
        catch (Exception exception)
        {
            statusMessage = $"Aircraft could not be saved: {exception.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task DeleteAircraftAsync()
    {
        if (selectedAircraft.Id == 0)
        {
            return;
        }

        var aircraftName = selectedAircraft.Name;

        try
        {
            var deleted = await AircraftService.DeleteAsync(selectedAircraft.Id);

            if (!deleted)
            {
                statusMessage = "Aircraft could not be found.";
                return;
            }

            await LoadAircraftAsync();
            StartNewAircraft();
            statusMessage = $"{aircraftName} was deleted.";
        }
        catch (Exception exception)
        {
            statusMessage = $"Aircraft could not be deleted: {exception.Message}";
        }
    }

    private void OpenDeleteLayoutOverlay()
    {
        deleteLayoutError = null;

        var availableLayout = availableLayouts.FirstOrDefault(layout =>
            string.Equals(layout.Definition.Key, selectedAircraft.CockpitLayoutKey, StringComparison.OrdinalIgnoreCase));

        if (availableLayout is null)
        {
            statusMessage = "The selected cockpit layout could not be found.";
            return;
        }

        if (!availableLayout.DatabaseId.HasValue)
        {
            statusMessage = "This installed layout has not been saved to the database, so there is no database record to delete.";
            return;
        }

        layoutPendingDeletion = availableLayout.Definition;
        layoutPendingDeletionDatabaseId = availableLayout.DatabaseId;

        affectedAircraft = aircraftList
            .Where(aircraft => string.Equals(aircraft.CockpitLayoutKey, layoutPendingDeletion.Key, StringComparison.OrdinalIgnoreCase))
            .Select(aircraft => new AffectedAircraftEditor
            {
                AircraftId = aircraft.Id,
                AircraftName = aircraft.Name,
                Resolution = AircraftLayoutResolution.Modify
            })
            .ToList();

        showDeleteLayoutOverlay = true;
    }

    private static Aircraft CreateEmptyAircraft()
    {
        return new Aircraft
        {
            EngineCount = 1,
            FuelTankCount = 2,
            BrakeCount = 2,

            LandingGearConfig = CreateDefaultLandingGear(),

            IsActive = true
        };
    }
    private void AddVsiCalibrationPoint()
    {
        advancedVsi?
            .CalibrationPoints
            .Add(
                new VsiCalibrationPointEditor
                {
                    VerticalSpeed = 0,
                    Angle = -90
                });
    }
    private void RemoveVsiCalibrationPoint(
        VsiCalibrationPointEditor point)
    {
        advancedVsi?
            .CalibrationPoints
            .Remove(point);
    }
    private void ResetVsiCalibration()
    {
        var defaults =
            CreateDefaultVsiLayout();

        advancedVsi =
            new VsiAdvancedEditor
            {
                MinimumVerticalSpeed =
                    defaults.MinimumVerticalSpeed,

                MaximumVerticalSpeed =
                    defaults.MaximumVerticalSpeed,

                LagSeconds =
                    defaults.LagSeconds,

                CalibrationPoints =
                    defaults.CalibrationPoints
                        .Select(point =>
                            new VsiCalibrationPointEditor
                            {
                                VerticalSpeed =
                                    point.VerticalSpeed,

                                Angle =
                                    point.Angle
                            })
                        .ToList()
            };
    }
    private void HandleLayoutSelection(
    ChangeEventArgs args)
    {
        var selectedValue =
            args.Value?.ToString();

        if (selectedValue == "__create_new__")
        {
            OpenLayoutOverlay();
            return;
        }

        selectedAircraft.CockpitLayoutKey =
            selectedValue ?? string.Empty;

        LoadAdvancedVsi();
    }

    private bool LayoutExists(string layoutKey)
    {
        return availableLayouts.Any(layout => string.Equals(layout.Definition.Key, layoutKey, StringComparison.OrdinalIgnoreCase));
    }

    private void OpenLayoutOverlay()
    {
        isEditingLayout = false;
        originalLayoutKey = null;
        editingLayoutDatabaseId = null;
        editingLayoutIsBuiltIn = false;
        layoutBeingEdited = null;

        layoutEditor = CreateEmptyLayoutEditor();
        layoutEditorError = null;
        showLayoutOverlay = true;
    }

    private void OpenEditLayoutOverlay()
    {
        var availableLayout = availableLayouts.FirstOrDefault(layout =>
            string.Equals(layout.Definition.Key, selectedAircraft.CockpitLayoutKey, StringComparison.OrdinalIgnoreCase));

        if (availableLayout is null)
        {
            statusMessage = "The selected cockpit layout could not be found.";
            return;
        }

        var currentLayout = availableLayout.Definition;

        isEditingLayout = true;
        originalLayoutKey = currentLayout.Key;
        editingLayoutDatabaseId = availableLayout.DatabaseId;
        editingLayoutIsBuiltIn = availableLayout.IsBuiltIn;
        layoutBeingEdited = currentLayout;
        layoutEditor = CreateEditorFromLayout(currentLayout);
        layoutEditorError = null;
        showLayoutOverlay = true;
    }

    private static CockpitLayoutEditor CreateEditorFromLayout(CockpitLayoutDefinition layout)
    {
        var definitionsByType = layout.Instruments.ToDictionary(instrument => instrument.Type);

        var instrumentEditors = Enum.GetValues<InstrumentType>()
            .Select(type =>
            {
                if (definitionsByType.TryGetValue(type, out var definition))
                {
                    return new InstrumentPlacementEditor
                    {
                        Type = type,
                        IsSelected = true,
                        GridRow = definition.GridRow,
                        GridColumn = definition.GridColumn,
                        RowSpan = definition.RowSpan,
                        ColumnSpan = definition.ColumnSpan,
                        ControlId = definition.ControlId,
                        DisplayName = definition.DisplayName,
                        IsVoiceControllable =
                            definition.IsVoiceControllable,
                        VoiceAliases =
                            definition.VoiceAliases?.ToList() ?? [],
                        VoiceCommands =
                            definition.VoiceCommands?
                                .Select(CloneCommand)
                                .ToList() ?? []
                    };
                }

                return new InstrumentPlacementEditor
                {
                    Type = type,
                    IsSelected = false,
                    GridRow = 1,
                    GridColumn = 1,
                    RowSpan = 1,
                    ColumnSpan = 1
                };
            })
            .ToList();

        return new CockpitLayoutEditor
        {
            Name = layout.Name,
            Rows = layout.Rows,
            Columns = layout.Columns,
            Instruments = instrumentEditors
        };
    }

    private void CloseLayoutOverlay()
    {
        showLayoutOverlay = false;
        layoutEditorError = null;
        isEditingLayout = false;
        originalLayoutKey = null;
        editingLayoutDatabaseId = null;
        editingLayoutIsBuiltIn = false;
        layoutBeingEdited = null;
    }

    private void DimensionsChanged()
    {
        layoutEditor.Rows = Math.Clamp(layoutEditor.Rows, 1, 10);
        layoutEditor.Columns = Math.Clamp(layoutEditor.Columns, 1, 10);

        foreach (var instrument in layoutEditor.Instruments)
        {
            instrument.GridRow = Math.Clamp(instrument.GridRow, 1, layoutEditor.Rows);
            instrument.GridColumn = Math.Clamp(instrument.GridColumn, 1, layoutEditor.Columns);
            instrument.RowSpan = Math.Clamp(instrument.RowSpan, 1, layoutEditor.Rows - instrument.GridRow + 1);
            instrument.ColumnSpan = Math.Clamp(instrument.ColumnSpan, 1, layoutEditor.Columns - instrument.GridColumn + 1);
        }
    }

    private void InstrumentSelectionChanged(InstrumentPlacementEditor instrument)
    {
        if (!instrument.IsSelected)
        {
            return;
        }

        var occupiedCells = layoutEditor.Instruments
            .Where(item => item.IsSelected && !ReferenceEquals(item, instrument))
            .SelectMany(GetOccupiedCells)
            .ToHashSet();

        for (var row = 1; row <= layoutEditor.Rows; row++)
        {
            for (var column = 1; column <= layoutEditor.Columns; column++)
            {
                if (occupiedCells.Contains((row, column)))
                {
                    continue;
                }

                instrument.GridRow = row;
                instrument.GridColumn = column;
                instrument.RowSpan = 1;
                instrument.ColumnSpan = 1;
                return;
            }
        }

        instrument.GridRow = 1;
        instrument.GridColumn = 1;
    }

    private static IEnumerable<(int Row, int Column)> GetOccupiedCells(InstrumentPlacementEditor instrument)
    {
        for (var row = instrument.GridRow; row < instrument.GridRow + instrument.RowSpan; row++)
        {
            for (var column = instrument.GridColumn; column < instrument.GridColumn + instrument.ColumnSpan; column++)
            {
                yield return (row, column);
            }
        }
    }

    private bool ValidateLayoutEditor()
    {
        layoutEditorError = null;

        var requiredInstruments =
            new[]
            {
                InstrumentType.Rudder,
                InstrumentType.Throttle,
                InstrumentType.Brake,
                InstrumentType.FireSuppression
            };

        if (string.IsNullOrWhiteSpace(layoutEditor.Name))
        {
            layoutEditorError = "Enter a name for the cockpit layout.";
            return false;
        }

        if (layoutEditor.Rows is < 1 or > 10)
        {
            layoutEditorError = "The layout must contain between 1 and 10 rows.";
            return false;
        }

        if (layoutEditor.Columns is < 1 or > 10)
        {
            layoutEditorError = "The layout must contain between 1 and 10 columns.";
            return false;
        }

        var selectedInstruments = layoutEditor.Instruments.Where(instrument => instrument.IsSelected).ToList();

        if (selectedInstruments.Count == 0)
        {
            layoutEditorError = "Select at least one instrument.";
            return false;
        }

        var occupiedCells = new Dictionary<(int Row, int Column), InstrumentType>();

        foreach (var instrument in selectedInstruments)
        {
            if (instrument.GridRow < 1 || instrument.GridColumn < 1)
            {
                layoutEditorError = $"{GetInstrumentDisplayName(instrument.Type)} must have a valid row and column.";
                return false;
            }

            var finalRow = instrument.GridRow + instrument.RowSpan - 1;
            var finalColumn = instrument.GridColumn + instrument.ColumnSpan - 1;

            if (finalRow > layoutEditor.Rows || finalColumn > layoutEditor.Columns)
            {
                layoutEditorError = $"{GetInstrumentDisplayName(instrument.Type)} extends outside the cockpit grid.";
                return false;
            }

            foreach (var cell in GetOccupiedCells(instrument))
            {
                if (occupiedCells.TryGetValue(cell, out var existingType))
                {
                    layoutEditorError = $"{GetInstrumentDisplayName(instrument.Type)} overlaps {GetInstrumentDisplayName(existingType)} at row {cell.Row}, column {cell.Column}.";
                    return false;
                }

                occupiedCells[cell] = instrument.Type;
            }
        }

        foreach (var required in requiredInstruments)
        {
            if (!layoutEditor.Instruments.Any(
                    instrument =>
                        instrument.Type == required))
            {
                layoutEditorError = $"The cockpit layout must contain {GetInstrumentDisplayName(required)}.";
                return false;
            }
        }

        return true;
    }

    private async Task SaveCockpitLayout()
    {
        if (!ValidateLayoutEditor())
        {
            return;
        }

        var newKey = isEditingLayout && editingLayoutIsBuiltIn && !string.IsNullOrWhiteSpace(originalLayoutKey)
            ? originalLayoutKey
            : CreateLayoutKey(layoutEditor.Name);

        var keyBelongsToAnotherLayout = availableLayouts.Any(layout =>
            string.Equals(layout.Definition.Key, newKey, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(layout.Definition.Key, originalLayoutKey, StringComparison.OrdinalIgnoreCase));

        if (keyBelongsToAnotherLayout)
        {
            layoutEditorError = "Another cockpit layout already uses this name.";
            return;
        }

        var savedLayout = new CockpitLayoutDefinition
        {
            Key = newKey,
            Name = layoutEditor.Name.Trim(),
            Rows = layoutEditor.Rows,
            Columns = layoutEditor.Columns,
            Instruments = layoutEditor.Instruments
                .Where(instrument => instrument.IsSelected)
                .Select(instrument =>
                    new InstrumentDefinition
                    {
                        Type = instrument.Type,
                        GridRow = instrument.GridRow,
                        GridColumn = instrument.GridColumn,
                        RowSpan = instrument.RowSpan,
                        ColumnSpan = instrument.ColumnSpan,
                        ControlId = instrument.ControlId,
                        DisplayName = instrument.DisplayName,
                        IsVoiceControllable =
                            instrument.IsVoiceControllable,
                        VoiceAliases =
                            instrument.VoiceAliases?.ToList() ?? [],
                        VoiceCommands =
                            instrument.VoiceCommands?
                                .Select(CloneCommand)
                                .ToList() ?? []
                    })
                .ToList(),
            AircraftId = layoutBeingEdited?.AircraftId ?? 0,
            EngineCount = layoutBeingEdited?.EngineCount ?? 1,

            Airspeed =
                layoutBeingEdited?.Airspeed
                ?? CreateDefaultAirspeedLayout(),

            ArtificialHorizon =
                layoutBeingEdited?.ArtificialHorizon
                ?? CreateDefaultArtificialHorizonLayout(),

            VSI = CreateVsiFromAdvancedEditor(),

            DefaultState =
                layoutBeingEdited?.DefaultState
                ?? CreateDefaultAircraftState()
        };

        try
        {
            if (isEditingLayout)
            {
                if (string.IsNullOrWhiteSpace(originalLayoutKey))
                {
                    layoutEditorError = "The original cockpit layout key is unavailable.";
                    return;
                }

                var databaseLayout = ConvertToDatabaseLayout(savedLayout);
                databaseLayout.IsBuiltIn = editingLayoutIsBuiltIn;

                await CockpitLayoutService.SaveEditedAsync(databaseLayout, editingLayoutDatabaseId, originalLayoutKey);
            }
            else
            {
                var databaseLayout = ConvertToDatabaseLayout(savedLayout);
                await CockpitLayoutService.CreateAsync(databaseLayout);
            }

            await LoadLayoutsAsync();
            await LoadAircraftAsync();

            selectedAircraft.CockpitLayoutKey = savedLayout.Key;
            statusMessage = isEditingLayout ? $"{savedLayout.Name} was updated." : $"{savedLayout.Name} was created and saved.";

            showLayoutOverlay = false;
            isEditingLayout = false;
            originalLayoutKey = null;
            editingLayoutDatabaseId = null;
            editingLayoutIsBuiltIn = false;
            layoutEditorError = null;
        }
        catch (ArgumentException exception)
        {
            layoutEditorError = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            layoutEditorError = exception.Message;
        }
        catch (KeyNotFoundException exception)
        {
            layoutEditorError = exception.Message;
        }
        catch (Exception exception)
        {
            layoutEditorError = "The cockpit layout could not be saved: " + exception.Message;
        }

        await LoadLayoutsAsync();
        await LoadAircraftAsync();

        if (selectedAircraft.Id != 0)
        {
            var refreshedAircraft = await AircraftService.GetByIdAsync(selectedAircraft.Id);

            if (refreshedAircraft is not null)
            {
                selectedAircraft = refreshedAircraft;
                selectedAircraftId = refreshedAircraft.Id;
            }
        }
        else
        {
            selectedAircraft.CockpitLayoutKey = savedLayout.Key;
        }
    }
    private async Task SaveAdvancedVsiAsync()
    {
        if (advancedVsi is null)
        {
            return;
        }

        var availableLayout =
            availableLayouts.FirstOrDefault(layout =>
                string.Equals(
                    layout.Definition.Key,
                    selectedAircraft.CockpitLayoutKey,
                    StringComparison.OrdinalIgnoreCase));

        if (availableLayout is null)
        {
            return;
        }

        var updatedLayout =
            CloneLayoutDefinition(
                availableLayout.Definition);

        updatedLayout.VSI =
            CreateVsiFromAdvancedEditor();

        var databaseLayout =
            ConvertToDatabaseLayout(updatedLayout);

        databaseLayout.IsBuiltIn =
            availableLayout.IsBuiltIn;

        await CockpitLayoutService.SaveEditedAsync(
            databaseLayout,
            availableLayout.DatabaseId,
            availableLayout.Definition.Key);
    }

    private static string CreateLayoutKey(string name)
    {
        var characters = name.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        var key = new string(characters);

        while (key.Contains("--", StringComparison.Ordinal))
        {
            key = key.Replace("--", "-", StringComparison.Ordinal);
        }

        return key.Trim('-');
    }

    private string GetPreviewGridStyle()
    {
        return $"grid-template-columns: repeat({layoutEditor.Columns}, minmax(0, 1fr));grid-template-rows: repeat({layoutEditor.Rows}, minmax(80px, 1fr));";
    }

    private static string GetPreviewInstrumentStyle(InstrumentPlacementEditor instrument)
    {
        return $"grid-row: {instrument.GridRow} / span {instrument.RowSpan}; grid-column: {instrument.GridColumn} / span {instrument.ColumnSpan};";
    }

    private static string GetInstrumentEditorClass(InstrumentPlacementEditor instrument)
    {
        return instrument.IsSelected ? "instrument-editor selected" : "instrument-editor";
    }

    private static string GetInstrumentDisplayName(InstrumentType type)
    {
        return type switch
        {
            InstrumentType.AirspeedIndicator => "Airspeed Indicator",
            InstrumentType.ArtificialHorizon => "Artificial Horizon",
            InstrumentType.Altimeter => "Altimeter",
            InstrumentType.TurnCoordinator => "Turn Coordinator",
            InstrumentType.HeadingIndicator => "Heading Indicator",
            InstrumentType.VerticalSpeedIndicator => "Vertical Speed Indicator",
            _ => type.ToString()
        };
    }

    private void CloseDeleteLayoutOverlay()
    {
        showDeleteLayoutOverlay = false;
        layoutPendingDeletion = null;
        layoutPendingDeletionDatabaseId = null;
        affectedAircraft = [];
        deleteLayoutError = null;
    }

    private void SetAircraftResolution(AffectedAircraftEditor affected, AircraftLayoutResolution resolution)
    {
        affected.Resolution = resolution;

        if (resolution == AircraftLayoutResolution.Delete)
        {
            affected.ReplacementLayoutKey = string.Empty;
        }
    }

    private IEnumerable<CockpitLayoutDefinition> GetReplacementLayouts()
    {
        if (layoutPendingDeletion is null)
        {
            return [];
        }

        return availableLayouts
            .Select(layout => layout.Definition)
            .Where(layout => !string.Equals(layout.Key, layoutPendingDeletion.Key, StringComparison.OrdinalIgnoreCase))
            .OrderBy(layout => layout.Name);
    }

    private bool ValidateLayoutDeletion()
    {
        deleteLayoutError = null;

        foreach (var affected in affectedAircraft)
        {
            if (affected.Resolution == AircraftLayoutResolution.Modify && string.IsNullOrWhiteSpace(affected.ReplacementLayoutKey))
            {
                deleteLayoutError = $"Select a replacement layout for {affected.AircraftName}.";
                return false;
            }

            if (affected.Resolution == AircraftLayoutResolution.Modify && string.Equals(affected.ReplacementLayoutKey, layoutPendingDeletion?.Key, StringComparison.OrdinalIgnoreCase))
            {
                deleteLayoutError = $"{affected.AircraftName} cannot be reassigned to the layout being deleted.";
                return false;
            }
        }

        return true;
    }

    private async Task ConfirmDeleteLayoutAsync()
    {
        if (layoutPendingDeletion is null || !layoutPendingDeletionDatabaseId.HasValue)
        {
            return;
        }

        if (!ValidateLayoutDeletion())
        {
            return;
        }

        isDeletingLayout = true;
        deleteLayoutError = null;

        var deletedLayoutName = layoutPendingDeletion.Name;
        var deletedLayoutKey = layoutPendingDeletion.Key;
        var selectedAircraftWasDeleted = affectedAircraft.Any(affected => affected.AircraftId == selectedAircraft.Id && affected.Resolution == AircraftLayoutResolution.Delete);

        try
        {
            var resolutions = affectedAircraft
                .Select(affected => new CockpitLayoutAircraftResolution
                {
                    AircraftId = affected.AircraftId,
                    Action = affected.Resolution == AircraftLayoutResolution.Delete ? AircraftResolutionAction.Delete : AircraftResolutionAction.Modify,
                    ReplacementLayoutKey = affected.Resolution == AircraftLayoutResolution.Modify ? affected.ReplacementLayoutKey : null
                })
                .ToList();

            await CockpitLayoutService.DeleteWithResolutionsAsync(layoutPendingDeletionDatabaseId.Value, deletedLayoutKey, resolutions);

            await LoadLayoutsAsync();
            await LoadAircraftAsync();

            if (selectedAircraftWasDeleted)
            {
                StartNewAircraft();
            }
            else if (selectedAircraft.Id != 0)
            {
                var refreshedAircraft = await AircraftService.GetByIdAsync(selectedAircraft.Id);

                if (refreshedAircraft is not null)
                {
                    selectedAircraft = refreshedAircraft;
                    selectedAircraftId = refreshedAircraft.Id;
                }
                else
                {
                    StartNewAircraft();
                }
            }
            else if (string.Equals(selectedAircraft.CockpitLayoutKey, deletedLayoutKey, StringComparison.OrdinalIgnoreCase))
            {
                selectedAircraft.CockpitLayoutKey = string.Empty;
            }

            CloseDeleteLayoutOverlay();
            statusMessage = $"{deletedLayoutName} was deleted.";
        }
        catch (ArgumentException exception)
        {
            deleteLayoutError = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            deleteLayoutError = exception.Message;
        }
        catch (KeyNotFoundException exception)
        {
            deleteLayoutError = exception.Message;
        }
        catch (Exception exception)
        {
            deleteLayoutError = "The cockpit layout could not be deleted: " + exception.Message;
        }
        finally
        {
            isDeletingLayout = false;
        }
    }
    private static InstrumentDefinition CloneInstrument(
        InstrumentDefinition instrument)
    {
        return new InstrumentDefinition
        {
            Type = instrument.Type,
            GridRow = instrument.GridRow,
            GridColumn = instrument.GridColumn,
            RowSpan = instrument.RowSpan,
            ColumnSpan = instrument.ColumnSpan,

            ControlId = instrument.ControlId,
            DisplayName = instrument.DisplayName,

            IsVoiceControllable =
                instrument.IsVoiceControllable,

            VoiceAliases =
                instrument.VoiceAliases?.ToList() ?? [],

            VoiceCommands =
                instrument.VoiceCommands?
                    .Select(CloneCommand)
                    .ToList() ?? []
        };
    }

    private static CockpitControlCommandDefinition CloneCommand(
        CockpitControlCommandDefinition command)
    {
        return new CockpitControlCommandDefinition
        {
            Command = command.Command,

            VoiceAliases =
                command.VoiceAliases?.ToList() ?? [],

            MinimumValue = command.MinimumValue,
            MaximumValue = command.MaximumValue,
            Unit = command.Unit,

            RequiresNumericValue =
                command.RequiresNumericValue
        };
    }

    private static CockpitLayoutDefinition CloneLayoutDefinition(
        CockpitLayoutDefinition layout)
    {
        return new CockpitLayoutDefinition
        {
            Key = layout.Key,
            Name = layout.Name,

            AircraftId =
                layout.AircraftId,

            Rows =
                layout.Rows,

            Columns =
                layout.Columns,

            EngineCount =
                layout.EngineCount,

            Instruments =
                layout.Instruments
                    .Select(CloneInstrument).ToList(),

            Airspeed =
                NormalizeAirspeedLayout(
                    layout.Airspeed),

            ArtificialHorizon =
                layout.ArtificialHorizon,

            VSI =
                layout.VSI,

            DefaultState =
                layout.DefaultState
        };
    }

    private static CockpitLayoutModel ConvertToDatabaseLayout(CockpitLayoutDefinition definition)
    {
        return new CockpitLayoutModel
        {
            Key = definition.Key,
            Name = definition.Name,
            IsBuiltIn = false,
            Details = new CockpitLayoutDetails
            {
                AircraftId = definition.AircraftId,
                Rows = definition.Rows,
                Columns = definition.Columns,
                Instruments = definition.Instruments
                    .Select(CloneInstrument)
                    .ToList(),
                EngineCount = definition.EngineCount,
                Airspeed = definition.Airspeed,
                ArtificialHorizon = definition.ArtificialHorizon,
                VSI = definition.VSI,
                DefaultState = definition.DefaultState
            }
        };
    }

    private static AirspeedIndicatorLayout CreateDefaultAirspeedLayout()
    {
        return new AirspeedIndicatorLayout
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

    private static ArtificialHorizonLayout CreateDefaultArtificialHorizonLayout()
    {
        return new ArtificialHorizonLayout
        {
            MinimumPitch = -30,
            MaximumPitch = 30,
            MinimumBank = -100,
            MaximumBank = 100
        };
    }

    private static VerticalSpeedIndicatorLayout CreateDefaultVsiLayout()
    {
        return new VerticalSpeedIndicatorLayout
        {
            MinimumVerticalSpeed = -2000,
            MaximumVerticalSpeed = 2000,
            LagSeconds = 6,
            CalibrationPoints =
            [
                new(-2000, -235),
                new(-1500, -200),
                new(-1000, -160),
                new(-500, -125),
                new(0, -90),
                new(500, -55),
                new(1000, -20),
                new(1500, 20),
                new(2000, 55)
            ]
        };
    }

    private static AircraftStateDefaults CreateDefaultAircraftState()
    {
        return new AircraftStateDefaults
        {
            CruiseAirspeed = 115,
            CruiseAltitude = 3000,
            DefaultHeading = 240,
            DefaultVerticalSpeed = 0,
            DefaultPitch = 2,
            DefaultBank = 0,
            NormalEnginePower = 75,
            FuelPercentage = 75
        };
    }

    private sealed class AvailableLayout
    {
        public int? DatabaseId { get; init; }
        public CockpitLayoutDefinition Definition { get; init; } = default!;
        public bool IsBuiltIn { get; init; }
        public bool IsSaved => DatabaseId.HasValue;
    }

    private sealed class AffectedAircraftEditor
    {
        public int AircraftId { get; set; }
        public string AircraftName { get; set; } = string.Empty;
        public AircraftLayoutResolution Resolution { get; set; } = AircraftLayoutResolution.Modify;
        public string ReplacementLayoutKey { get; set; } = string.Empty;
    }

    private enum AircraftLayoutResolution
    {
        Modify,
        Delete
    }

    private sealed class CockpitLayoutEditor
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; } = 2;
        public int Columns { get; set; } = 3;
        public List<InstrumentPlacementEditor> Instruments { get; set; } = [];
    }

    private sealed class InstrumentPlacementEditor
    {
        public InstrumentType Type { get; set; }

        public bool IsSelected { get; set; }

        public int GridRow { get; set; } = 1;

        public int GridColumn { get; set; } = 1;

        public int RowSpan { get; set; } = 1;

        public int ColumnSpan { get; set; } = 1;

        public string ControlId { get; set; } =
            string.Empty;

        public string DisplayName { get; set; } =
            string.Empty;

        public bool IsVoiceControllable { get; set; }

        public List<string> VoiceAliases { get; set; } = [];

        public List<CockpitControlCommandDefinition>
            VoiceCommands
        { get; set; } = [];
    }

    private static CockpitLayoutEditor CreateEmptyLayoutEditor()
    {
        return new CockpitLayoutEditor
        {
            Name = string.Empty,
            Rows = 2,
            Columns = 3,

            Instruments = Enum
                .GetValues<InstrumentType>()
                .Select(type => new InstrumentPlacementEditor
                {
                    Type = type,

                    GridRow = 1,
                    GridColumn = 1,
                    RowSpan = 1,
                    ColumnSpan = 1
                })
                .ToList()
        };
    }
}