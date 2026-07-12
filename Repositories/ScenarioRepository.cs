using AeroResponse.Data;
using AeroResponse.Models;

namespace AeroResponse.Repositories;

public class ScenarioRepository(ApplicationDbContext context)
    : EfGenericRepository<EmergencyScenario>(context);