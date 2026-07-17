using System.Text.Json;
using AeroResponse.Models;
using Microsoft.JSInterop;

namespace AeroResponse.Services;

public class SimulationSelectionStorage
{
    private const string StorageKey = "aeroresponse.simulation.selection";

    private readonly IJSRuntime _jsRuntime;

    public SimulationSelectionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<SimulationSelection?> GetAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            StorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SimulationSelection>(json);
        }
        catch (JsonException)
        {
            await ClearAsync();
            return null;
        }
    }

    public async ValueTask SaveAsync(SimulationSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.IsValid)
        {
            throw new ArgumentException(
                "AircraftKey and ScenarioType are required.",
                nameof(selection));
        }

        var json = JsonSerializer.Serialize(selection);

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            StorageKey,
            json);
    }

    public ValueTask ClearAsync()
    {
        return _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            StorageKey);
    }
}