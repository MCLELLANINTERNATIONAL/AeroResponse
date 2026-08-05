using AeroResponse.Data;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AeroResponse.Repositories;

public class AircraftRepository : EfGenericRepository<Aircraft>
{
    private readonly ApplicationDbContext context;
    private readonly ILogger<AircraftRepository> logger;

    public AircraftRepository(
        ApplicationDbContext context,
        ILogger<AircraftRepository> logger)
        : base(context)
    {
        this.context = context;
        this.logger = logger;
    }

    public override Task<Aircraft?> GetByIdAsync(int id)
    {
        return context.Aircraft
            .Include(a => a.LandingGearConfig)
            .ThenInclude(c => c.Units)
            .FirstOrDefaultAsync(a => a.Id == id)!;
    }

    public override async Task UpdateAsync(Aircraft aircraft)
    {
        ArgumentNullException.ThrowIfNull(aircraft);


        var existing = await context.Aircraft
            .Include(a => a.LandingGearConfig)
            .ThenInclude(c => c.Units)
            .FirstOrDefaultAsync(a => a.Id == aircraft.Id);

        if (existing is null)
            throw new InvalidOperationException("Aircraft not found.");

        existing.Name = aircraft.Name;
        existing.Manufacturer = aircraft.Manufacturer;
        existing.AircraftType = aircraft.AircraftType;
        existing.Description = aircraft.Description;
        existing.MaxAltitude = aircraft.MaxAltitude;
        existing.CruiseSpeed = aircraft.CruiseSpeed;
        existing.EngineCount = aircraft.EngineCount;
        existing.FuelTankCount = aircraft.FuelTankCount;
        existing.BrakeCount = aircraft.BrakeCount;
        existing.CockpitLayoutKey = aircraft.CockpitLayoutKey;
        existing.IsActive = aircraft.IsActive;

        existing.LandingGearConfig.Kind = aircraft.LandingGearConfig.Kind;

        existing.LandingGearConfig = new AircraftLandingGearConfig
        {
            Kind = aircraft.LandingGearConfig.Kind,
            Units = aircraft.LandingGearConfig.Units
                .OrderBy(u => u.Order)
                .Select(u => new LandingGearUnit
                {
                    Number = u.Number,
                    Label = u.Label,
                    Position = u.Position,
                    Status = u.Status,
                    Order = u.Order
                })
                .ToList()
        };
        await context.SaveChangesAsync();
    }
    public async Task<Aircraft?> GetByIdWithLandingGearAsync(int id)
    {
        return await context.Aircraft
            .Include(a => a.LandingGearConfig)
            .ThenInclude(c => c.Units)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}