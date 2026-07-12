using AeroResponse.Data;
using AeroResponse.Models;

namespace AeroResponse.Repositories;

public class AircraftRepository(ApplicationDbContext context)
    : EfGenericRepository<Aircraft>(context);